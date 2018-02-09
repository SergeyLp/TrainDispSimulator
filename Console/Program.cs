using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
//using NLog;

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

    public class DecelerationDistance {
        public DecelerationDistance() {
            sp = new SpeedPoint();
            begin = new PointPos();
        }
        public SpeedPoint sp { get; set; }
        public PointPos begin { get; set; }
        //public double begin { get; set; }
        //public double target_speed { get; set; }
    };

    class Ai {
        const double a_loco = 0.15;    // m/s^2
        const double speed_max_loco_restrict = 120 / 3.6; // m/s

        Queue<DecelerationDistance> decelerations_distances;
        //double end_deceleration = 0;
        DecelerationDistance deceleration_distance;

        double current_speed_restict = 0;   // m/s
        double a = 0;

        SpeedPoint current_sp;
        Queue<DecelerationDistance> current_decelerations = new Queue<DecelerationDistance>();
        readonly Queue<SpeedPoint> speed_points;

        internal Ai(Queue<SpeedPoint> speed_points_) {
            speed_points = speed_points_;

            decelerations_distances = new Queue<DecelerationDistance>();

            buildPrepareForSpeedrestrictTable();

            deceleration_distance = decelerations_distances.Dequeue();
            current_sp = speed_points.Dequeue();
        }

        void buildPrepareForSpeedrestrictTable() {
            Trace.WriteLine("Table prepare speed restrict");
            double prev_speed_restrict = 0;
            foreach (var restrict in speed_points) {
                double delta_speed = restrict.speed - prev_speed_restrict;
                if (delta_speed < 0) {
                    double t = -delta_speed / a_loco;   // Time for deceleration
                    double space = prev_speed_restrict * t + a_loco * t * t / 2;
                    DecelerationDistance dec_dist = new DecelerationDistance {
                        sp = restrict,
                        begin = new PointPos(restrict.pos - space)
                    };
                    decelerations_distances.Enqueue(dec_dist);
                    Trace.Write(string.Format("{0} {1} {2:##0.0} {3:##0.0}\n", prev_speed_restrict * 3.6, restrict.speed * 3.6, (restrict.pos - space) / 1000, restrict.pos / 1000));
                }
                prev_speed_restrict = restrict.speed;
            }
        }

        internal double getAcceleration(double pos, double v) {
            void TracePosSpeed(string header) =>
                Trace.WriteLine(String.Format("{0} at   {1:##0.000}  {2:##0.0}",header, pos / 1000, v * 3.6)); //{0,-8:t} ti
            
            if (pos >= current_sp.pos) {
                current_speed_restict = (current_sp.speed > speed_max_loco_restrict)
                                ? speed_max_loco_restrict
                                : current_sp.speed;
                Console.Write("\t[{0}]\n", current_sp.speed * 3.6);
                if (speed_points.Any()) {
                    current_sp = speed_points.Dequeue();
                } else {
                    current_sp.pos = 99e99;
                }
                if (a == 0)
                    if (!current_decelerations.Any()) {
                        a = +a_loco;
                        TracePosSpeed("A+");
                    } else if (current_decelerations.Count == 1) {
                        if (v < current_decelerations.Peek().sp.speed) {
                            a = a + a_loco;
                            TracePosSpeed("A~");
                        }
                    }
            }

            if (a < 0 && (pos > deceleration_distance.begin.pos || v < deceleration_distance.sp.speed)) {
                a = 0;
                TracePosSpeed("A0");
            }

            if (v > current_speed_restict - 0.05 && a > 0) {
                TracePosSpeed("A\\");
                a = 0;
            }

            if (pos >= deceleration_distance.begin.pos) {
                current_decelerations.Enqueue(deceleration_distance);
                TracePosSpeed("C+");
                //end_deceleration = deceleration_distance.begin.pos;
                if (decelerations_distances.Count > 0) {
                    deceleration_distance = decelerations_distances.Dequeue();
                } else {
                    deceleration_distance.begin.pos = 99e99;
                }

                TracePosSpeed("A-");
                a = -a_loco;
            }

            if (current_decelerations.Any() && pos > current_decelerations.Peek().sp.pos) {
                current_decelerations.Dequeue();
                if (!current_decelerations.Any()) {
                    TracePosSpeed("A^");
                    a = +a_loco;
                }
                TracePosSpeed("C-");
            }

            if (v > current_speed_restict + 0.1) {
                Console.Write("  {0:##0.0} !\n", current_speed_restict * 3.6);
                TracePosSpeed(String.Format("*! {0}", current_speed_restict * 3.6));
            }
            return a;
        }
    }

    public class Driver { }
    public class RollingStock {

    }
    public class Locomotion : RollingStock { }
    public class Wagon : RollingStock { }

    public class Consist { }


    class Game {

        static void Main(string[] args){

            Branch branch = new Branch();
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

            Ai ai = new Ai(speed_points);

            DateTime t0 = new DateTime(2009, 5, 1, 7, 18, 0);
            Console.Write("{0}\n", t0);

            double v = 0, v0 =0;    // m/s
            double dp, pos = 0; // m
            double sec_from_0 = 0;  // s
            NamedPoint next_station = stations.Dequeue();

            for (; ; ) {
                DateTime ti = t0.AddSeconds(sec_from_0);

                double a = ai.getAcceleration(pos, v);

                v = v0 + a * dt;
                dp = (v0 + v)/2 * dt ;
                pos += dp;
                v0 = v;

                Console.Write("\r{0,-8:t}{1,7:##0.000}{2,7:##0.0}", ti, pos/1000, v * 3.6);

                if (pos >= next_station.pos) {
                    Console.Write("  {0}\n", next_station.name);
                    if (stations.Any())
                        next_station = stations.Dequeue();
                    else
                        break;
                }
                //Thread.Sleep(5);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
