using Engine;
using Botclient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Globalization;

namespace Server
{
    public class Stats
    {
        Database Database;
        string StatsPath;

        public Stats(Database db)
        {
            Database = db;
            StatsPath = db.DbProperties.Path + db.DbProperties.PathSeparator + "Stats.csv";

            if (!File.Exists(StatsPath))
            {
                string[] statContent = new string[]
                {
                    "Timestamp",
                    "Cycles processed",
                    "Games processed",
                    "Database size",
                    "Database length",
                    "Search speed",
                    "Processing time",

                    "Intelligence Alice vs random Regular",
                    "Intelligence Bob vs random Regular",
                    "Intelligence Alice vs min-max Regular",
                    "Intelligence Bob vs min-max Regular",

                    "Intelligence Alice vs random Ranked",
                    "Intelligence Bob vs random Ranked",
                    "Intelligence Alice vs min-max Ranked",
                    "Intelligence Bob vs min-max Ranked",

                    "Win percentage Alice vs random",
                    "Win percentage Bob vs random",
                    "Win percentage Alice vs min-max",
                    "Win percentage Bob vs min-max",
                };

                File.WriteAllText(StatsPath, string.Join(",", statContent));

                addCurrentMeasurement(0);
            }
        }

        public void addCurrentMeasurement(long processingTime)
        {
            DateTime timeStamp = DateTime.Now;
            int cyclesProcessed = Database.DbProperties.getCycles();
            int gamesProcessed = Database.DbProperties.getGames();
            long databaseSize = Database.getDatabaseSize();
            int databaseLength = Database.getDatabaseLength();

            double searchSpeed = calculateSearchSpeed();

            double rndAliceIntelligenceReR = calculateIntelligence(new Random_bot(players.Bob), RateMethod.RegularRating);
            double rndBobIntelligenceReR = calculateIntelligence(new Random_bot(players.Alice), RateMethod.RegularRating);
            double minMaxAliceIntelligenceReR = calculateIntelligence(new Minmax_bot(players.Bob, 2), RateMethod.RegularRating);
            double minMaxBobIntelligenceReR = calculateIntelligence(new Minmax_bot(players.Alice, 2), RateMethod.RegularRating);

            double rndAliceIntelligenceRaR = calculateIntelligence(new Random_bot(players.Bob), RateMethod.RankRating);
            double rndBobIntelligenceRaR = calculateIntelligence(new Random_bot(players.Alice), RateMethod.RankRating);
            double minMaxAliceIntelligenceRaR = calculateIntelligence(new Minmax_bot(players.Bob, 2), RateMethod.RankRating);
            double minMaxBobIntelligenceRaR = calculateIntelligence(new Minmax_bot(players.Alice, 2), RateMethod.RankRating);

            double rndAliceWinPerc = getWinPercentage(new Random_bot(players.Bob));
            double rndBobWinPerc = getWinPercentage(new Random_bot(players.Alice));
            double minMaxAliceWinPerc = getWinPercentage(new Minmax_bot(players.Bob, 2));
            double minMaxBobWinPerc = getWinPercentage(new Minmax_bot(players.Alice, 2));

            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            string[] newData = new string[]
            {
                timeStamp.Ticks.ToString(),
                cyclesProcessed.ToString(),
                gamesProcessed.ToString(),
                databaseSize.ToString(),
                databaseLength.ToString(),
                searchSpeed.ToString(nfi),
                processingTime.ToString(),

                rndAliceIntelligenceReR.ToString(nfi),
                rndBobIntelligenceReR.ToString(nfi),
                minMaxAliceIntelligenceReR.ToString(nfi),
                minMaxBobIntelligenceReR.ToString(nfi),

                /*rndAliceIntelligenceRaR.ToString(nfi),
                rndBobIntelligenceRaR.ToString(nfi),
                minMaxAliceIntelligenceRaR.ToString(nfi),
                minMaxBobIntelligenceRaR.ToString(nfi),*/
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,

                rndAliceWinPerc.ToString(nfi),
                rndBobWinPerc.ToString(nfi),
                //minMaxAliceWinPerc.ToString(nfi),
                //minMaxBobWinPerc.ToString(nfi),
                string.Empty,
                string.Empty,
            };

            string content = string.Join(",", newData);

            File.AppendAllText(StatsPath, "\n" + content);
        }

        internal double getWinPercentage(IPlayer opponent)
        {
            int totalGames = 100;
            int wonGames = 0;

            for (int i = 0; i < totalGames; i++)
            {
                Game g = new Game(Database.DbProperties.FieldWidth, Database.DbProperties.FieldHeight);

                while (g.has_won(players.Empty) && g.stones_count != g.width * g.height)
                {
                    byte column = 0;

                    if (opponent.player == g.next_player)
                    {
                        column = opponent.get_turn(g.get_field());
                    }
                    else
                    {
                        column = Database.get_column(g.get_field());
                    }

                    string info = string.Empty;
                    g.add_stone(column, g.next_player, ref info);
                }

                players dbPlayer = opponent.player == players.Alice ? players.Bob : players.Alice;
                if (g.has_won(dbPlayer))
                {
                    wonGames++;
                }
            }

            return (double)wonGames / (double)totalGames;
        }

        internal double calculateIntelligence(IPlayer opponent, RateMethod rm)
        {
            double intelligence = 0;
            double totalMoves = 0;

            while (totalMoves < 180)
            {
                Game g = new Game(Database.DbProperties.FieldWidth, Database.DbProperties.FieldHeight);

                while (g.has_won(players.Empty) && g.stones_count != g.width * g.height)
                {
                    byte column = 0;

                    if (opponent.player == g.next_player)
                    {
                        column = opponent.get_turn(g.get_field());
                    }
                    else
                    {
                        column = Database.get_column(g.get_field());
                        int[] inputRatings = g.get_field().rate_columns(g.next_player, 2, true);

                        double moveIntel = 0;

                        switch (rm)
                        {
                            case RateMethod.RegularRating:
                                moveIntel = regularRate(inputRatings, column);
                                break;
                            case RateMethod.RankRating:
                                moveIntel = rankedRating(inputRatings, column);
                                break;
                                
                        }

                        if (!double.IsNaN(moveIntel))
                        {
                            intelligence += moveIntel;
                            totalMoves++;
                        }
                    }

                    string info = string.Empty;
                    g.add_stone(column, g.next_player, ref info);
                }
            }

            intelligence /= totalMoves;

            return intelligence;
        }

        private double regularRate(int[] inputRatings, byte column)
        {
            int[] ratings = inputRatings.
                Select(r => r == int.MinValue || r == int.MaxValue ? 0 : Math.Abs(r)).ToArray();

            int scoreShift = -ratings.Min();
            int maxRating = ratings.Max() + scoreShift;

            double chosenRating = ratings[column] + scoreShift;
            return chosenRating / maxRating;
        }

        private double rankedRating(int[] inputRatings, byte column)
        {
            int[] ratings = inputRatings.
                Select(r => r == int.MinValue || r == int.MaxValue ? 0 : Math.Abs(r)).ToArray();

            int[] existingRatings = ratings.Distinct().OrderBy(r => r).ToArray();

            if (existingRatings.Length > 1)
            {
                int colScore = ratings[column];

                for (int i = 0; i < existingRatings.Length; i++)
                {
                    if (existingRatings[i] == colScore)
                        return i / (existingRatings.Length - 1);
                }

                return double.NaN;
            }
            else
            {
                return 1;
            }
        }

        private double calculateSearchSpeed()
        {
            double time = 0;
            Random rnd = new Random();

            int searchLocations = 20;
            int tries = 10;

            foreach (DatabaseSegment dbSeg in Database.Segments)
            {
                int fieldCount = dbSeg.FieldCount;
                for (int i = 0; i < searchLocations; i++)
                {
                    if (dbSeg.FieldCount > 0)
                    {
                        int checkPosition = rnd.Next(fieldCount);
                        Field f = dbSeg.readField(checkPosition);

                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        for (int j = 0; j < tries; j++)
                        {
                            dbSeg.findField(f);
                        }
                        sw.Stop();

                        time += sw.Elapsed.TotalSeconds;
                    }
                }
            }

            double averageTime = time / (searchLocations * tries);
            double speed = 1 / averageTime;
            return speed;
        }

        internal enum RateMethod
        {
            RegularRating,
            RankRating
        }
    }
}
