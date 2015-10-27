using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    class Program
    {
        static void Main(string[] args)
        {
            var recSys = new RecommenderSystem();

            recSys.CheckRecSystemForErrors();

            recSys.PrintRecommendationsFor("ec6dfcf19485cb011e0b22637075037aae34cf26");
        }
    }
}
