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
    /// <summary>
    /// This class manages the statistics for a database. The statistics can be used for research.
    /// </summary>
    public class Stats
    {
        Database Database;  // The database the stats belong to.
        string StatsPath;   // Path where all stats are saved (in .csv format).

        public Stats(Database db)
        {
            Database = db;
            StatsPath = db.DbProperties.Path + db.DbProperties.PathSeparator + "Stats.csv";

            if (!File.Exists(StatsPath))
            {
                // Creates a new stats file, if it doesn't exist already.
                string[] statContent = new string[] // This array is the standard
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

                    "Win percentage Alice vs random",
                    "Win percentage Bob vs random",
                };

                File.WriteAllText(StatsPath, string.Join(",", statContent));

                addCurrentMeasurement(0);
            }
        }

        /// <summary>
        /// Adds a new point in the stats file, with all current info about the database and its learning progression.
        /// </summary>
        /// <param name="processingTime">The time it took to process the data from the games of the last cycles.</param>
        public void addCurrentMeasurement(long processingTime)
        {
            DateTime timeStamp = DateTime.Now;
            int cyclesProcessed = Database.DbProperties.getCycles();
            int gamesProcessed = Database.DbProperties.getGames();
            long databaseSize = Database.getDatabaseSize();
            int databaseLength = Database.getDatabaseLength();

            double searchSpeed = calculateSearchSpeed();

            double rndAliceIntelligence = calculateIntelligence(new Random_bot(players.Bob), RateMethod.RegularRating);
            double rndBobIntelligence = calculateIntelligence(new Random_bot(players.Alice), RateMethod.RegularRating);
            double minMaxAliceIntelligence = calculateIntelligence(new Minmax_bot(players.Bob, 2), RateMethod.RegularRating, 1);
            double minMaxBobIntelligence = calculateIntelligence(new Minmax_bot(players.Alice, 2), RateMethod.RegularRating, 1);
            
            double rndAliceWinPerc = getWinPercentage(new Random_bot(players.Bob));
            double rndBobWinPerc = getWinPercentage(new Random_bot(players.Alice));

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

                rndAliceIntelligence.ToString(nfi),
                rndBobIntelligence.ToString(nfi),
                minMaxAliceIntelligence.ToString(nfi),
                minMaxBobIntelligence.ToString(nfi),
                
                rndAliceWinPerc.ToString(nfi),
                rndBobWinPerc.ToString(nfi),
            };

            string content = string.Join(",", newData);

            File.AppendAllText(StatsPath, "\n" + content);
        }

        /// <summary>
        /// Returns the win percentage of the database bot against the given opponent.
        /// </summary>
        /// <param name="opponent"></param>
        /// <returns></returns>
        internal double getWinPercentage(IPlayer opponent)
        {
            int totalGames = 1000;
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

        /// <summary>
        /// Calculates the intelligence based on our own rating method, based on playing games against the given opponent. (Optional argument is the amount of games to play)
        /// </summary>
        /// <param name="opponent"></param>
        /// <param name="rm"></param>
        /// <param name="maxGames"></param>
        /// <returns>Intelligence percentage (as a double)</returns>
        internal double calculateIntelligence(IPlayer opponent, RateMethod rm, int maxGames = -1)
        {
            double intelligence = 0;
            double totalMoves = 0;
            int games = 0;

            while (totalMoves < 180 && (maxGames == -1 || games < maxGames))
            {
                games++;
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
                        int[] inputRatings = g.get_field().rate_columns(g.next_player, 2);

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

        /// <summary>
        /// Rates the decision made by the database, based on the inputratings (generated by the min-max algorithm).
        /// </summary>
        /// <param name="inputRatings">Ratings generated by min-max algorithm</param>
        /// <param name="column">Chosen column</param>
        /// <returns>Rating percentage (as a double)</returns>
        private double regularRate(int[] inputRatings, byte column)
        {
            int[] ratings = inputRatings.
                Select(r => r == int.MinValue || r == int.MaxValue ? 0 : Math.Abs(r)).ToArray();

            int scoreShift = -ratings.Min();
            int maxRating = ratings.Max() + scoreShift;

            double chosenRating = ratings[column] + scoreShift;
            return chosenRating / maxRating;
        }

        /// <summary>
        /// Rates the decision made by the database by using a ranking system, based on the inputratings (generated by the min-max algorithm).
        /// </summary>
        /// <param name="inputRatings">Ratings generated by min-max algorithm</param>
        /// <param name="column">Chosen column</param>
        /// <returns>Rating percentage (as a double)</returns>
        [Obsolete]
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

        /// <summary>
        /// Calculates the search speed, based on an average of search speeds in different database segments and different items within these segments.
        /// </summary>
        /// <returns>The search speed in items per second.</returns>
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
            RankRating  // Obsolete
        }
    }
}
