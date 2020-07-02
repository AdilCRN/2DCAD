using System;
using System.Text;

namespace MarkGeometriesLib.Classes
{
    public static class PerformanceHelper
    {
        public static string Compare(Action actionAIn, Action actionBIn, int compareRepeat = 5, int timingRepeat = 5, string tagA = "A", string tagB = "B")
        {
            double scoreA = 0;
            double scoreB = 0;
            var rand = new Random();

            for (int i = 0; i < compareRepeat; i++)
            {
                if (rand.NextDouble() > 0.5)
                {
                    scoreA += Timeit(actionAIn, timingRepeat);
                    scoreB += Timeit(actionBIn, timingRepeat);
                }
                else
                {
                    scoreB += Timeit(actionBIn, timingRepeat);
                    scoreA += Timeit(actionAIn, timingRepeat);
                }
            }

            return Compare(
                tagA, tagB, scoreA / compareRepeat, scoreB / compareRepeat
            );
        }

        public static string Compare(string tagA, string tagB, double valA, double valB, int precision = 8)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Comparing {tagA}: {Math.Round(valA, precision)}ms and {tagB}: {Math.Round(valB, precision)}ms");

            double pDiff = Math.Round((Math.Abs(valB - valA) / (0.5 * (valA + valB))) * 100, precision);

            if (valA < valB)
            {
                sb.AppendLine($"{tagA} is {Math.Round(pDiff, precision)}% faster than {tagB}");
            }
            else
            {
                sb.AppendLine($"{tagB} is {Math.Round(pDiff, precision)}% faster than {tagA}");
            }

            return sb.ToString();
        }

        public static double Timeit(Action actionIn, int howmany = 5)
        {
            var startTime = DateTime.Now;

            for (int i = 0; i < howmany; i++)
                actionIn.Invoke();

            var totalTime = (DateTime.Now - startTime).TotalMilliseconds / howmany;
            return totalTime;
        }
    }
}
