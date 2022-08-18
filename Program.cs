using System;
using System.IO;

namespace ExamTask
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            string currentDir = Directory.GetCurrentDirectory();

            string site = currentDir + @"\site";

            Http server = new Http(site, 8888);
        }
    }
}
