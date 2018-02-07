using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace Ralway {

    public class Branch {

        public class NamedNode {
            public enum RType {
                block,
                junction,
            };
            public RType type_ { get; set; }
            public NamedNode prev { get; set; }
            public NamedNode next { get; set; }
            public string name { get; set; }

            public NamedNode(string new_name, RType type = RType.block, Block prev = null, Block next = null) {
                name = new_name;
                type_ = type;
                this.prev = prev;
                this.next = next;
            }
        }

        public class Block : NamedNode {
            private double len_;

            public Block(string name, double len, RType type = RType.block, Block prev = null, Block next = null)
                : base(name, type, prev, next) => this.len = len;

            public double len { get => len_; set => len_ = value; }

            public new string ToString() => String.Format("{0,12} {1,4}", name, len.ToString("0.0"));
        }

        public LinkedList<Block> list = new LinkedList<Block>();

        public void Dump() {
            double dist = 0;
            Trace.WriteLine("Branch dump");
            foreach (var li in list) {
                string s = li.ToString();

                dist += li.len;
                s += "\t";
                s += dist.ToString();
                Trace.WriteLine(String.Format("{0}  {1,5}", li.ToString(), dist.ToString("0.0")));
            }
        }

        public void AddElement(string name, double len) {
            Block el = new Block(name, len);
            list.AddLast(el);
        }
    }


    public class PointPos {
        public double pos { get; set; }
        public PointPos(PointPos po) => pos = po.pos;
        public PointPos(double po) => pos = po;
        public PointPos() { }
    }

    public class NamedPoint: PointPos{
        public string name { get; set; }
    }

    public class SpeedPoint : PointPos{
        public double speed { get; set; }
    }

    public class DecelerationDistance /* : PointPos*/{
        public SpeedPoint sp { get; set; }
        public PointPos begin { get; set; }
        //public double begin { get; set; }
        //public double target_speed { get; set; }
    };

    class Game {
        static void Main(string[] args){
            Branch branch = new Branch();

            const double a_loco = 0.15;    // m/s^2
            const double speed_max_loco_restrict = 120 / 3.6; // m/s
            const double dt = 0.05;    // s

            Queue<NamedPoint> stations = new Queue<NamedPoint>();
            using (StreamReader reader = File.OpenText(@"..\..\..\RW\Выборг.layout")){
                string text;
                double last_dist = 0;
                while ((text = reader.ReadLine()) != null) {
                    if (text.Length < 1 || text[0] == '#') {
                        continue;
                    }
                    string[] data = text.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 2) continue;
                    NamedPoint np = new NamedPoint();
                    np.name = data[0].Replace('_', ' ');
                    np.pos = double.Parse(data[2]) * 1000; //m
                    double length = np.pos - last_dist;
                    last_dist = np.pos;
                    branch.AddElement(np.name, length);
                    stations.Enqueue(np);
                }
                //branch.Dump();
            }

            Queue<SpeedPoint> speed_points = new Queue<SpeedPoint>();
            using (StreamReader reader = File.OpenText(@"..\..\..\RW\Выборг.speed")) {
                string text;
                while ((text = reader.ReadLine() )!= null) {
                    text = text.Trim();
                    if (text.Length < 1 || text.StartsWith("#")) {
                        continue;
                    }
                    string[] data = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 2) continue;
                    SpeedPoint sp = new SpeedPoint();
                    sp.pos = double.Parse(data[0]) * 1000;
                    sp.speed = double.Parse(data[1])/3.6 ;
                    speed_points.Enqueue(sp);
                }
            }

            Trace.WriteLine("Table prepare speed restrict");
            Queue<DecelerationDistance> dist_decelerations = new Queue<DecelerationDistance>();
            double prev_speed_restrict = 0;
            foreach (var sp in speed_points) {
                double speed_restrict = sp.speed;//speed_map[dist];
                double delta_speed = speed_restrict - prev_speed_restrict;
                if (delta_speed < 0) {
                    double t = -delta_speed / a_loco;
                    double space = prev_speed_restrict * t + a_loco * t * t / 2;
                    DecelerationDistance dec_dist = new DecelerationDistance();
                    //dec_dist.begin = pp;
                    SpeedPoint spp = new SpeedPoint();
                    dec_dist.sp = spp;
                    dec_dist.sp.pos = sp.pos;
                    dec_dist.sp.speed = sp.speed;
                    PointPos pp = new PointPos(sp.pos - space);
                    dec_dist.begin = pp;
                    Trace.Write(String.Format("{0} {1} {2:##0.0} {3:##0.0}\n", prev_speed_restrict * 3.6, speed_restrict * 3.6, (sp.pos - space)/1000, sp.pos / 1000));
                    dist_decelerations.Enqueue(dec_dist);
                }
                prev_speed_restrict = speed_restrict;
            }

            DateTime date0 = new DateTime(2009, 5, 1, 7, 18, 0);
            Console.Write("{0}\n", date0);

            double v = 0, v0 =0;    // m/s
            double dp = 0, pos = 0; // m
            double sec_from_0 = 0;  // s
            double current_speed_restict = 0;   // m/s
            DecelerationDistance space_deceleration = dist_decelerations.Dequeue();
            NamedPoint next_station = stations.Dequeue();
            double a = a_loco;
            double end_deceleration = 0;
            SpeedPoint current_sp = speed_points.Dequeue();
            Queue<DecelerationDistance> current_decelerations = new Queue<DecelerationDistance>();
            for (; ; ) {
                DateTime date = date0.AddSeconds(sec_from_0);

                if (pos >= current_sp.pos) { 
                    current_speed_restict = (current_sp.speed > speed_max_loco_restrict)
                                    ? speed_max_loco_restrict
                                    : current_sp.speed;
                    Console.Write("\t[{0}]\n", current_sp.speed*3.6);
                    if (speed_points.Any()){
                        current_sp = speed_points.Dequeue();
                    } else {
                        current_sp.pos = 99e99;
                    }
                    if (a == 0)
                        if (!current_decelerations.Any()) {
                            a = +a_loco;
                            Trace.WriteLine(String.Format("A+ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                        } else if (current_decelerations.Count == 1){
                            if (v < current_decelerations.Peek().sp.speed) {
                                a = a + a_loco;
                                Trace.WriteLine(String.Format("A~ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                            }
                                
                        }
                }

                if (a < 0 && (pos > space_deceleration.begin.pos || v < space_deceleration.sp.speed) ) { 
                    a = 0;
                    Trace.WriteLine(String.Format("A0 at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                }

                if (v > current_speed_restict - 0.05 && a > 0) {
                    Trace.WriteLine(String.Format("A\\ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    a = 0;
                }

                if (pos >= space_deceleration.begin.pos) {
                    current_decelerations.Enqueue(space_deceleration);
                    Trace.WriteLine(String.Format("C+ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    end_deceleration = space_deceleration.begin.pos;
                    if (dist_decelerations.Count > 0) {
                        space_deceleration = dist_decelerations.Dequeue();
                    } else {
                        space_deceleration.begin.pos = 99e99;
                    }

                    Trace.WriteLine(String.Format("A- at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    a = -a_loco;
                }

                if (current_decelerations.Any() && pos > current_decelerations.Peek().sp.pos) {
                    current_decelerations.Dequeue();
                    if (!current_decelerations.Any()) {
                        Trace.WriteLine(String.Format("A^ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                        a = +a_loco;
                    }
                    Trace.WriteLine(String.Format("C- at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                }

                v = v0 + a * dt;
                dp = (v0 + v)/2 * dt ;
                pos += dp;
                v0 = v;

                Console.Write("\r{0,-8:t}{1,7:##0.000}{2,7:##0.0}", date, pos/1000, v * 3.6);

                    if (pos >= next_station.pos) {
                        Console.Write("  {0}\n", next_station.name);
                        if (!stations.Any())
                            break;
                        else
                            next_station = stations.Dequeue();
                    } else {
                        if (v > current_speed_restict + 0.1) {
                        Console.Write("  {0:##0.0} !\n", current_speed_restict * 3.6);
                        Trace.WriteLine(String.Format("*! at {0,-8:t}  {1:##0.000}  {2:##0.0}  {3:##0.0}", date, pos / 1000, v * 3.6, current_speed_restict * 3.6));
                        v = current_speed_restict;
                    }
                }
                //Thread.Sleep(5);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
