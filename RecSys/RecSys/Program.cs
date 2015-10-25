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
            var sw = new System.Diagnostics.Stopwatch();
            var recSys = new RecommenderSystem();

            sw.Start();
            recSys.ReadCounts("training.set");
            sw.Stop();


            Console.WriteLine("Read subset: {0}", sw.Elapsed);

            sw.Restart();
            recSys.ReadSimilarities("similarities.bin");
            sw.Stop();


            Console.WriteLine("Read similarities: {0}", sw.Elapsed);

            sw.Restart();
            recSys.ReadTestingSet("testing.set");
            sw.Stop();

            Console.WriteLine("Read testing set: {0}", sw.Elapsed);

            sw.Restart();
            recSys.AddPredictionsToTestingSet();
            sw.Stop();

            Console.WriteLine("Predictions: {0}", sw.Elapsed);

            sw.Restart();
            var mae = recSys.MeanAverageError();
            var rmse = recSys.RootMeanAverageError();
            sw.Stop();

            Console.WriteLine("MAE: {0}, RMSE: {1}, counted in {2}", mae, rmse, sw.Elapsed);

            Console.Read();
        }

        static void Main2(string[] args)
        {
            var sw = new System.Diagnostics.Stopwatch();
            
            var recSys = new RecommenderSystem();

            sw.Start();
            recSys.ReadCounts("training.set");
            sw.Stop();

            Console.WriteLine("Read: {0}", sw.Elapsed);

            Console.WriteLine("Reading have ended");

            sw.Restart();
            recSys.CalculateNorms();
            sw.Stop();

            Console.WriteLine("Norm: {0}", sw.Elapsed);
            
            Console.WriteLine("Calculate norms have ended");

            sw.Restart();
            recSys.CalculateSimilarities(recSys.CosSimilarity);
            sw.Stop();

            Console.WriteLine("Similarity: {0}", sw.Elapsed);

            recSys.WriteSimilarities("similarities.bin");
        }

        static void Main1(string[] args)
        {
            var sw = new System.Diagnostics.Stopwatch();
            var recSys = new RecommenderSystem();

            sw.Start();
            recSys.SeparateBySongs("subset.set", 0.8);
            sw.Stop();


            Console.WriteLine("Separate: {0}", sw.Elapsed);
        }
    }
}
