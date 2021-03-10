using System;
using System.IO;
using System.Threading;

namespace _
{
    class Program
    {
        //This is the the method that prints the animation out to the screen. The first argument specifies the time between frames. The second specifies the file to read from
        public static void Animation(int delay, string file)
        {
            string aline;
            string filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            filePath = Directory.GetParent(Directory.GetParent(filePath).FullName).FullName;
            filePath = Directory.GetParent(Directory.GetParent(filePath).FullName).FullName;

            StreamReader frame = new StreamReader(filePath + "/" + @file);
            while (!frame.EndOfStream)
            {
                aline = frame.ReadLine();
                if (aline == "break") //when adding animations, put all the frames into a single .txt file, and add only the word "break" on it's own line inbetween
                {
                    Thread.Sleep(delay);
                    Console.Clear();
                }
                else
                {
                    Console.WriteLine(aline);
                }
            }

            //for (int i = 0; i < framenumber; i++)
            //{
                              
            //    //assigns the file to be read based on the current frame
            //    switch (i)
            //    {
            //        default:
            //            file = file1;
            //            break;
            //        case 1:
            //            file = file2;
            //            break;
            //        case 2:
            //            file = file3;
            //            break;
            //        case 3:
            //            file = file4;
            //            break;
            //        case 4:
            //            file = file5;
            //            break;
            //    }
            //    Console.Clear();
            //    StreamReader frame = new StreamReader(@file);
            //    string everyline = frame.ReadToEnd();
            //    Console.WriteLine(everyline);
                
            //    frame.Close();
            //    Thread.Sleep(delay);
            //}
        }
        static void Main(string[] args)
        {
            Animation(1500, "test.txt");
            Console.Read();
        }
    }
}
