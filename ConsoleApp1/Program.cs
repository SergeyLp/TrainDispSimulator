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

            //branch.Dump();

            DateTime date0 = new DateTime(2009, 5, 1, 16, 30, 0);
            Console.Write("{0}\n", date0);

            IList<double> time_points = stations_map.Keys;
            int station_number = 0;

            const double v_max_loc = 120 / 3.6;
            IList<double> speed_points = speed_map.Keys;
            int speed_index = 0;
            double v = 0;
            double dp = 0;
            double sec_from_0 = 0;
            double dt = 0.1;
            double pos = 0;
            for (; ; ) {
                
                if ((speed_index < speed_points.Count) && (pos >= speed_points[speed_index] * 1000)) {
                    v = speed_map[speed_points[speed_index++]] / 3.6;
                    v = (v < v_max_loc) ? v : v_max_loc;
                }

                dp = v * dt;
                pos += dp;

                DateTime date = date0.AddSeconds(sec_from_0);
                Console.Write("{0:t}\t{1:##0.0}\t{2,3}", date, pos/1000, v * 3.6);
                if (pos >= time_points[station_number] * 1000) {
                    Console.Write("  {0}\n", stations_map[time_points[station_number]]);
                    if (++station_number == time_points.Count) break;
                } else
                    Console.Write("\r");
                //Thread.Sleep(10);
                sec_from_0 += dt;
            }
            Console.Write("\n\t\t<<PRESS ENTER>>");
            Console.ReadKey();

        }
    }
}
