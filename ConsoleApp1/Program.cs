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
            public RType type_;
            public NamedNode prev_ { get; set; } //указатели узла
            public NamedNode next_ { get; set; }
            public string name_ { get; set; }  //вставляемое значение
        }

        public class Block : NamedNode {
            public double len_;

            public Block(string name, double len, RType type = RType.block, Block prev = null, Block next = null) {
                name_ = name;
                len_ = len;
                type_ = type;
                prev_ = prev;
                next_ = next;
            }

            public new string ToString() {
                return String.Format("{0,12} {1,4}", name_, len_.ToString("0.0"));
            }
        }

        public LinkedList<Block> list = new LinkedList<Block>();

        public  void Dump() {
            double dist = 0;
            foreach(var li in list) {
                string s = li.ToString();

                dist += li.len_;
                s += "\t";
                s += dist.ToString();
                Console.WriteLine(String.Format("{0}  {1,5}", li.ToString(), dist.ToString("0.0") ));
            }
        }

        public void AddElement(string name, double len) {
            Block el = new Block(name, len);
            list.AddLast(el);
        }
    }


    class Program{


        static void Main(string[] args){
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Branch branch = new Branch();
            double last_dist = 0;
            SortedList<double, string> stations_map = new SortedList<double, string>();
            using (StreamReader reader = File.OpenText(@"D:\Win7\lis\Documents\Dev\RW\Выборг.layout")){
                string text;
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

            double a_loc = 0.25;    // m/s^2
            const double v_max_loc = 120 / 3.6; // m/s

            SortedList<double, double> speed_prepare_list = new SortedList<double, double>();
            double v_prev =0;
            foreach (var dist in speed_points) {
                double vx = speed_map[dist]/3.6;
                double dv = vx - v_prev;
                if (dv < 0) {
                    double t = -dv / a_loc;
                    double dist_prep = v_prev * t + a_loc * t * t / 2;
                    Console.Write("{0} {1} {2} {3}\n", v_prev * 3.6, vx * 3.6, dist - dist_prep/1000, dist_prep);
                    speed_prepare_list.Add(dist*1000 - dist_prep, vx);
                }
                v_prev = vx;
            }
            IList<double> speed_prep_points = speed_prepare_list.Keys;

            //branch.Dump();

            DateTime date0 = new DateTime(2009, 5, 1, 16, 30, 0);
            //Console.Write("{0}\n", date0);

            IList<double> time_points = stations_map.Keys;
            int station_number = 0;

            int speed_index = 0;
            double v = 0, v0 =0;    // m/s
            double dp = 0, pos = 0; // m
            double sec_from_0 = 0;  // s
            double dt = 0.05;    // s
            double speed_restict = 0;   // m/s
            double next_speed_restict = 0;   // m/s
            int speed_prep_index = 0;
            double speed_prep_dist = 0;
            double a = a_loc;
            for (; ; ) {
                
                if ((speed_index < speed_points.Count) && (pos >= speed_points[speed_index] * 1000)) {
                    speed_restict = speed_map[speed_points[speed_index++]] / 3.6;
                    a = +a_loc;
                    if (speed_index < speed_points.Count)
                        next_speed_restict = speed_map[speed_points[speed_index]] / 3.6;
                }

                if ((speed_prep_index < speed_prep_points.Count) && (pos >= speed_prep_points[speed_prep_index])) {
                    speed_prep_dist = speed_prep_points[speed_prep_index++];
                    a = -a_loc;
                }


                v = v0 + a * dt;

                if ((a < 0) && (v < next_speed_restict))
                    a = 0;

                v = (v <= speed_restict) ? v : speed_restict;
                v = (v <= v_max_loc) ? v : v_max_loc;

                dp = (v0 + v)/2 * dt ;
                pos += dp;
                v0 = v;

                DateTime date = date0.AddSeconds(sec_from_0);
                Console.Write("{0:T}\t{1:##0.00}\t{2,3}", date, pos/1000, (int)(v * 3.6));
                if (pos >= time_points[station_number] * 1000) {
                    Console.Write("  {0}\n", stations_map[time_points[station_number]]);
                    if (++station_number == time_points.Count) break;
                } else
                    Console.Write("\r");
                Thread.Sleep(5);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
