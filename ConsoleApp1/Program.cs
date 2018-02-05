using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace Ralway{

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

            public NamedNode(string new_name, RType type = RType.block, Block prev = null, Block next = null){
                name = new_name;
                type_ = type;
                this.prev = prev;
                this.next = next;
            }
        }

        public class Block : NamedNode {
            public double len_;

            public Block(string name, double len, RType type = RType.block, Block prev = null, Block next = null)
                :base(name, type, prev, next) {
                len_ = len;
            }

            public new string ToString() => String.Format("{0,12} {1,4}", name, len_.ToString("0.0"));
            
        }

        public LinkedList<Block> list = new LinkedList<Block>();

        public  void Dump() {
            double dist = 0;
            Trace.WriteLine("Branch dump");
            foreach (var li in list) {
                string s = li.ToString();

                dist += li.len_;
                s += "\t";
                s += dist.ToString();
                Trace.WriteLine(String.Format("{0}  {1,5}", li.ToString(), dist.ToString("0.0") ));
            }
        }

        public void AddElement(string name, double len) {
            Block el = new Block(name, len);
            list.AddLast(el);
        }
    }

    public struct SpaceDeceleration {
        public double begin;
        public double speed;
        public double end;

        public SpaceDeceleration(double begin_, double end_, double speed_) {
            begin = begin_;
            end = end_;
            speed = speed_;
        }
    };

    class Game {
        static void Main(string[] args){
            //Branch branch = new Branch();

            const double a_loco = 0.15;    // m/s^2
            const double speed_max_loco_restrict = 120 / 3.6; // m/s
            const double dt = 0.05;    // s

            SortedList<double, string> stations_map = new SortedList<double, string>();
            using (StreamReader reader = File.OpenText(@"D:\Win7\lis\Documents\Dev\RW\Выборг.layout")){
                string text;
                double last_dist = 0;
                while ((text = reader.ReadLine()) != null) {
                    if (text.Length < 1 || text[0] == '#') {
                        continue;
                    }
                    string[] data = text.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 2) continue;
                    string name = data[0].Replace('_', ' ');
                    double dist = double.Parse(data[2]) * 1000;
                    double length = dist - last_dist;
                    last_dist = dist;
                    //branch.AddElement(name, length);
                    stations_map.Add(dist, name);
                }
                //branch.Dump();
            }
            IList<double> time_points = stations_map.Keys;

            SortedList<double, double> speed_map = new SortedList<double, double>();
            using (StreamReader reader = File.OpenText(@"D:\Win7\lis\Documents\Dev\RW\Выборг.speed")) {
                string text;
                while ((text = reader.ReadLine() )!= null) {
                    text = text.Trim();
                    if (text.Length < 1 || text.StartsWith("#")) {
                        continue;
                    }
                    string[] data = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 2) continue;
                    double dist = double.Parse(data[0]) * 1000;
                    double speed = double.Parse(data[1])/3.6 ;
                    speed_map.Add(dist, speed);
                }
            }
            IList<double> speed_points = speed_map.Keys;

            Trace.WriteLine("Table prepare speed restrict");
            Queue<SpaceDeceleration> dist_decelerations = new Queue<SpaceDeceleration>();
            double prev_speed_restrict =0;
            foreach (var dist in speed_points) {
                double speed_restrict = speed_map[dist];
                double delta_speed = speed_restrict - prev_speed_restrict;
                if (delta_speed < 0) {
                    double t = -delta_speed / a_loco;
                    double space = prev_speed_restrict * t + a_loco * t * t / 2;
                    Trace.Write(String.Format("{0} {1} {2:##0.0} {3:##0.0}\n", prev_speed_restrict * 3.6, speed_restrict * 3.6, (dist - space)/1000, dist / 1000));
                    dist_decelerations.Enqueue(new SpaceDeceleration(dist - space, dist, speed_restrict) );
                }
                prev_speed_restrict = speed_restrict;
            }

            DateTime date0 = new DateTime(2009, 5, 1, 16, 30, 0);
            Console.Write("{0}\n", date0);

            int station_number = 0;
            int speed_index = 0;
            double v = 0, v0 =0;    // m/s
            double dp = 0, pos = 0; // m
            double sec_from_0 = 0;  // s
            double current_speed_restict = 0;   // m/s
            double next_speed_restict = 0;   // m/s
            SpaceDeceleration space_deceleration = dist_decelerations.Dequeue();
            double a = a_loco;
            double end_deceleration = 0;
            Queue<SpaceDeceleration> current_decelerations = new Queue<SpaceDeceleration>();
            for (; ; ) {
                DateTime date = date0.AddSeconds(sec_from_0);

                if ((speed_index < speed_points.Count) && (pos >= speed_points[speed_index])) {
                    current_speed_restict = speed_map[speed_points[speed_index++]] ;
                    current_speed_restict = (current_speed_restict > speed_max_loco_restrict)
                                    ? speed_max_loco_restrict
                                    : current_speed_restict;
                    if (a == 0 && !current_decelerations.Any()) {
                        a = +a_loco;
                        Trace.WriteLine(String.Format("A+ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    }
                    if (speed_index < speed_points.Count) {
                        next_speed_restict = speed_map[speed_points[speed_index]] ;
                        next_speed_restict = (next_speed_restict > speed_max_loco_restrict)
                                        ? speed_max_loco_restrict
                                        : next_speed_restict;
                    }
                }

                if (a < 0 && (pos > space_deceleration.begin || v < space_deceleration.speed) ) { 
                    a = 0;
                    Trace.WriteLine(String.Format("A0 at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                }

                if (v > current_speed_restict - 0.05 && a > 0) {
                    Trace.WriteLine(String.Format("A\\ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    a = 0;
                }

                if (pos >= space_deceleration.begin) {
                    current_decelerations.Enqueue(space_deceleration);
                    Trace.WriteLine(String.Format("C+ at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    end_deceleration = space_deceleration.begin;
                    if (dist_decelerations.Count > 0) {
                        space_deceleration = dist_decelerations.Dequeue();
                    } else {
                        space_deceleration.begin = 99e99;
                    }

                    Trace.WriteLine(String.Format("A- at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                    a = -a_loco;
                }

                if (current_decelerations.Any() && pos > current_decelerations.Peek().end) {
                    current_decelerations.Dequeue();
                    Trace.WriteLine(String.Format("C- at {0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos / 1000, v * 3.6));
                }

                v = v0 + a * dt;
                dp = (v0 + v)/2 * dt ;
                pos += dp;
                v0 = v;

                Console.Write("{0,-8:t}  {1:##0.000}  {2:##0.0}", date, pos/1000, v * 3.6);

                if (pos >= time_points[station_number]) {
                    Console.Write("  {0}\n", stations_map[time_points[station_number]]);
                    if (++station_number == time_points.Count) break;
                } else {
                    if (v > current_speed_restict + 0.5) {
                        Console.Write("  {0:##0.0} !\n", current_speed_restict * 3.6);
                        Trace.WriteLine(String.Format("*! at {0,-8:t}  {1:##0.000}  {2:##0.0}  {3:##0.0}", date, pos / 1000, v * 3.6, current_speed_restict * 3.6));
                        v = current_speed_restict;
                    } else
                        Console.Write("\r");
                }
                //Thread.Sleep(5);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
