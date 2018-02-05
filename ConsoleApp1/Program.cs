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


    class Game{
        static void Main(string[] args){
            Branch branch = new Branch();

            double a_loco = 0.2;    // m/s^2
            const double speed_max_loco_restrict = 120 / 3.6; // m/s

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
                    double dist = double.Parse(data[2]);
                    double length = dist - last_dist;
                    last_dist = dist;
                    branch.AddElement(name, length);
                    stations_map.Add(dist, name);
                }
            }

            SortedList<double, double> speed_map = new SortedList<double, double>();
            using (StreamReader reader = File.OpenText(@"D:\Win7\lis\Documents\Dev\RW\Выборг.speed")) {
                string text;
                while ((text = reader.ReadLine()) != null) {
                    if (text.Length < 1 || text[0] == '#') {
                        continue;
                    }
                    string[] data = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 2) continue;
                    double dist = double.Parse(data[0]);
                    double speed = double.Parse(data[1]);
                    speed_map.Add(dist, speed);
                }
            }
            IList<double> speed_points = speed_map.Keys;

            Trace.WriteLine("Table prepare speed restrict");
            Queue<double> dist_deceleration_q = new Queue<double>();
            double prev_speed_restrict =0;
            foreach (var dist in speed_points) {
                double speed_restrict = speed_map[dist]/3.6;
                double delta_speed = speed_restrict - prev_speed_restrict;
                if (delta_speed < 0) {
                    double t = -delta_speed / a_loco;
                    double space_deceleration = prev_speed_restrict * t + a_loco * t * t / 2;
                    Trace.Write(String.Format("{0} {1} {2:#,0.000} {3}\n", prev_speed_restrict * 3.6, speed_restrict * 3.6, dist - space_deceleration/1000, (int)space_deceleration));
                    dist_deceleration_q.Enqueue(dist * 1000 - space_deceleration);
                }
                prev_speed_restrict = speed_restrict;
            }

            branch.Dump();

            DateTime date0 = new DateTime(2009, 5, 1, 16, 30, 0);
            Console.Write("{0}\n", date0);

            IList<double> time_points = stations_map.Keys;
            int station_number = 0;

            int speed_index = 0;
            double v = 0, v0 =0;    // m/s
            double dp = 0, pos = 0; // m
            double sec_from_0 = 0;  // s
            double dt = 0.05;    // s
            double current_speed_restict = 0;   // m/s
            double next_speed_restict = 0;   // m/s
            double dist_deceleration = dist_deceleration_q.Dequeue(); ;
            double a = a_loco;
            for (; ; ) {
                if ((speed_index < speed_points.Count) && (pos >= speed_points[speed_index] * 1000)) {
                    current_speed_restict = speed_map[speed_points[speed_index++]] / 3.6;
                    current_speed_restict = (current_speed_restict > speed_max_loco_restrict)
                                    ? speed_max_loco_restrict
                                    : current_speed_restict;
                    a = +a_loco;
                    if (speed_index < speed_points.Count) {
                        next_speed_restict = speed_map[speed_points[speed_index]] / 3.6;
                        next_speed_restict = (next_speed_restict > speed_max_loco_restrict)
                                        ? speed_max_loco_restrict
                                        : next_speed_restict;

                    }
                }

                if (pos >= dist_deceleration) {
                    dist_deceleration = (dist_deceleration_q.Count > 0) ? dist_deceleration_q.Dequeue(): 9e99;
                    a = -a_loco;
                }

                v = v0 + a * dt;
                dp = (v0 + v)/2 * dt ;
                pos += dp;
                v0 = v;

                if ((a < 0) && (v <= next_speed_restict) && (v <= current_speed_restict))
                    a = 0;

                if (v >= current_speed_restict)
                    a = 0;

                DateTime date = date0.AddSeconds(sec_from_0);
                Console.Write("{0:t}\t{1:##0.000}\t{2,3}", date, pos/1000, (int)(v * 3.6));
                if (pos >= time_points[station_number] * 1000) {
                    Console.Write("  {0}\n", stations_map[time_points[station_number]]);
                    if (++station_number == time_points.Count) break;
                } else
                    Console.Write("\r");
                //Thread.Sleep(5);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
