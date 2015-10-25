using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RecSys
{
    class RecommenderSystem
    {
        private const int NeighboursCount = 20;
        private const float PenaltyParamenter = 5;

        private Dictionary<string, int> _usersToNumbers;
        private Dictionary<string, int> _songsToNumbers;

        private string[] _numbersToUsers;
        private string[] _numbersToSongs;

        private byte[][] _countsTable;

        private int[][] _songsOfUsers;

        private float[] _norms;

        private byte[][] _similaritiesTable;
        private byte[][] _predictionsTable;

        private int _userCount;
        private int _songCount;

        public List<Info> _testingSet;

        public RecommenderSystem()
        {
        }

        public void SeparateBySongs(string path, double probability, int randomParam = 0)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found");
                return;
            }

            var rnd = new Random(randomParam);
            var reader = new StreamReader(path);

            var folder = Path.GetDirectoryName(path);

            var testingWriter = new StreamWriter(string.Format("{0}{1}testing.set", folder, folder.Length == 0 ? string.Empty : "\\"));
            var trainingWriter = new StreamWriter(string.Format("{0}{1}training.set", folder, folder.Length == 0 ? string.Empty : "\\"));

            if (reader.EndOfStream)
            {
                Console.WriteLine("File is empty");
                return;
            }

            var line = reader.ReadLine();
            var parts = line.Split('\t');

            while (!reader.EndOfStream)
            {
                var user = parts[0];
                var linesOfUser = new List<string>();
                linesOfUser.Add(line);

                line = reader.ReadLine();
                parts = line.Split('\t');

                while (parts[0] == user && !reader.EndOfStream)
                {
                    linesOfUser.Add(line);
                    line = reader.ReadLine();
                    parts = line.Split('\t');
                }

                var trainingCount = (int)Math.Ceiling(probability * linesOfUser.Count);
                var testingDict = new SortedDictionary<int, string>();
                var trainingArr = linesOfUser.ToArray();
                var indexes = new int[linesOfUser.Count];
                for (int i = 0; i < linesOfUser.Count; ++i) indexes[i] = i;

                for (int i = linesOfUser.Count - 1; i >= trainingCount; --i)
                {                    
                    var rndVal = rnd.Next(i);
                    testingDict.Add(indexes[rndVal], linesOfUser[indexes[rndVal]]);
                    trainingArr[indexes[rndVal]] = null;

                    indexes[rndVal] = indexes[i];
                }

                foreach (var pair in testingDict) testingWriter.WriteLine(pair.Value);
                
                foreach (var str in trainingArr) 
                    if (str != null) trainingWriter.WriteLine(str);
            }

            reader.Close();
            testingWriter.Close();
            trainingWriter.Close();
        }

        public void ReadCounts(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found");
                return;
            }

            var reader = new StreamReader(path);
            var wholeSubset = new List<string[]>();

            while (!reader.EndOfStream)
                wholeSubset.Add(reader.ReadLine().Split('\t'));

            reader.Close();

            _userCount = 0;

            var allSongs = new HashSet<string>();
            _usersToNumbers = new Dictionary<string, int>();

            foreach (var line in wholeSubset)
            {
                if (!_usersToNumbers.ContainsKey(line[0]))
                    _usersToNumbers.Add(line[0], _userCount++);

                if (!allSongs.Contains(line[1]))
                    allSongs.Add(line[1]);
            }

            _songCount = allSongs.Count;

            Console.WriteLine("Users count: {0}", _userCount);
            Console.WriteLine("Songs count: {0}", _songCount);

            _numbersToUsers = new string[_userCount];

            _songsToNumbers = new Dictionary<string, int>(_songCount);

            foreach (var pair in _usersToNumbers)
                _numbersToUsers[pair.Value] = pair.Key;

            var allSongsList = allSongs.ToList();
            allSongsList.Sort();
            _numbersToSongs = allSongsList.ToArray();

            for (int i = 0; i < _songCount; ++i)
            {
                _songsToNumbers.Add(_numbersToSongs[i], i);
            }

            var songsOfUsers = new List<int>[_userCount];

            for (int i = 0; i < _userCount; ++i)
                songsOfUsers[i] = new List<int>();

            _countsTable = new byte[_userCount][];

            for (int i = 0; i < _userCount; ++i) 
                _countsTable[i] = new byte[_songCount];

            foreach (var line in wholeSubset)
            {
                var i = _usersToNumbers[line[0]];
                var j = _songsToNumbers[line[1]];
                var count = int.Parse(line[2]);
                songsOfUsers[i].Add(j);
                _countsTable[i][j] = Convert.ToByte(Math.Round(Math.Log(count, 2)) + 1);
            }

            _songsOfUsers = new int[_songCount][];

            for (int i = 0; i < _songCount; ++i)
                _songsOfUsers[i] = songsOfUsers[i].ToArray();
        }

        public void ReadTestingSet(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found");
                return;
            }

            var reader = new StreamReader(path);

            _testingSet = new List<Info>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine().Split('\t');
                var user = _usersToNumbers[line[0]];
                var song = _songsToNumbers[line[1]];
                var count = int.Parse(line[2]);
                var triplet = new Info(user, song, count);
                _testingSet.Add(triplet);
            }

            reader.Close();
        }

        public void CalculateNorms()
        {
            _norms = new float[_userCount];
            for (int i = 0; i < _userCount; ++i)
            {
                float norm = 0;
                foreach (var j in _songsOfUsers[i])
                {
                    norm += _countsTable[i][j] * _countsTable[i][j];
                }
                _norms[i] = (float) Math.Sqrt(norm);
            }
        }

        public void CalculateSimilarities(Func<int, int, byte> calculateSimilarity)
        {
            _similaritiesTable = new byte[_userCount][];

            for (int i = 0; i < _userCount; ++i)
            {
                _similaritiesTable[i] = new byte[i];

                for (int j = 0; j < i; ++j)
                {
                    _similaritiesTable[i][j] = calculateSimilarity(i, j);
                }

                if (i % 250 == 0)  Console.WriteLine("Count: {0}", i);
            }
        }

        public void AddPredictionsToTestingSet()
        {
            int count = 0;

            foreach (var entry in _testingSet)
            {
                var user = entry.User;
                var song = entry.Song;

                entry.LogPrediction = CalculateLogPrediction(user, song);
                count++;

                if (count % 1000 == 0) Console.WriteLine("Count: {0}", count);
            }
        }

        public float CalculateLogPrediction(int user, int song)
        {
            var users = new List<int>();

            for (int i = 0; i < _userCount; ++i)
                if (_countsTable[i][song] != 0) users.Add(i);

            var neighbours = users.OrderByDescending(otherUser => GetSimilarity(user, otherUser)).ToList().GetRange(0, Math.Min(NeighboursCount, users.Count));

            ////// DELETE
            // var neighboursSimilarity = neighbours.ConvertAll(otherUser => GetSimilarity(user, otherUser));
            //////

            float prediction = 0;
            float similaritySum = 0;

            foreach (var neighbour in neighbours)
            {
                var similarity = (float)GetSimilarity(user, neighbour) / 100;

                prediction += similarity * (float)_countsTable[neighbour][song];

                similaritySum += similarity;
            }

            if (similaritySum == 0) return 0;

            return prediction / similaritySum;
        }

        public void WriteSimilarities(string path)
        {
            var stream = new FileStream(path, FileMode.Create);
            var writer = new BinaryWriter(stream);

            foreach (var row in _similaritiesTable)
                foreach (var similarity in row)
                    writer.Write(similarity);

            writer.Close();
            stream.Close();
        }

        public void ReadSimilarities(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found");
                return;
            }

            var stream = new FileStream(path, FileMode.Open);
            var reader = new BinaryReader(stream);

             _similaritiesTable = new byte[_userCount][];

             for (int i = 0; i < _userCount; ++i)
             {
                 _similaritiesTable[i] = new byte[i];

                 for (int j = 0; j < i; ++j)
                 {
                     _similaritiesTable[i][j] = reader.ReadByte();
                 }
             }

             reader.Close();
             stream.Close();
        }

        public byte GetSimilarity(int user1, int user2)
        {
            return user1 < user2 ? _similaritiesTable[user2][user1] : _similaritiesTable[user1][user2];
        }

        public byte CosSimilarity(int user1, int user2)
        {
            float scalar = 0;

            int index1 = 0;
            int index2 = 0;

            int commonSongsCount = 0;

            var songsOfUser1 = _songsOfUsers[user1];
            var songsOfUser2 = _songsOfUsers[user2];

            while (index1 < songsOfUser1.Length && index2 < songsOfUser2.Length)
            {
                if (songsOfUser1[index1] > songsOfUser2[index2]) ++index2;
                else if (songsOfUser1[index1] < songsOfUser2[index2]) ++index1;
                else
                {
                    scalar += _countsTable[user1][songsOfUser1[index1++]] * _countsTable[user2][songsOfUser2[index2++]];
                    ++commonSongsCount;
                }
            }

            scalar /= _norms[user1] * _norms[user2];

            scalar *= (float)commonSongsCount / (float)(commonSongsCount + PenaltyParamenter);

            return Convert.ToByte(scalar * 100);
        }

        public byte PCSimilarity(int user1, int user2)
        {
            float scalar = 0;
            float normUser1 = 0;
            float normUser2 = 0;

            int index1 = 0;
            int index2 = 0;

            var songsOfUser1 = _songsOfUsers[user1];
            var songsOfUser2 = _songsOfUsers[user2];

            float user1AvgRate = 0;
            float user2AvgRate = 0;

            // Calculates average song counts
            foreach (var song in songsOfUser1)
            {
                user1AvgRate += _countsTable[user1][song];
            }
            user1AvgRate /= (float)songsOfUser1.Length;

            foreach (var song in songsOfUser2)
            {
                user2AvgRate += _countsTable[user2][song];
            }
            user2AvgRate /= (float)songsOfUser2.Length;

            // Calculates Pearson similarity 
            while (index1 < songsOfUser1.Length && index2 < songsOfUser2.Length)
            {
                if (songsOfUser1[index1] > songsOfUser2[index2]) ++index2;
                else if (songsOfUser1[index1] < songsOfUser2[index2]) ++index1;
                else
                {
                    scalar += (_countsTable[user1][songsOfUser1[index1]] - user1AvgRate) * (_countsTable[user2][songsOfUser2[index2]] - user2AvgRate);
                    normUser1 += (_countsTable[user1][songsOfUser1[index1]] - user1AvgRate) * (_countsTable[user1][songsOfUser1[index1]] - user1AvgRate);
                    normUser2 += (_countsTable[user2][songsOfUser2[index2]] - user2AvgRate) * (_countsTable[user2][songsOfUser2[index2]] - user2AvgRate);
                    index1++;
                    index2++;
                }
            }

            if (normUser1 == 0 || normUser2 == 0)
                return 0;

            scalar /= (float)Math.Sqrt(normUser1 * normUser2);
            return Convert.ToByte((scalar + 1) * 50);
        }

        public float MeanAverageError()
        {
            long errorSum = 0;

            foreach (var entry in _testingSet)
            {
                errorSum += Math.Abs(entry.Count - entry.Prediction);
            }

            var error = (double)errorSum / (double)_testingSet.Count;
 
            return (float)Math.Sqrt(error);
        }

        public float RootMeanAverageError()
        {
            long errorSum = 0;

            foreach (var entry in _testingSet)
            {
                errorSum += (entry.Count - entry.Prediction) * (entry.Count - entry.Prediction);
            }

            var error = (double)errorSum / (double)_testingSet.Count;

            return (float)Math.Sqrt(error);
        }
    }

    class Info
    {
        public int User;
        public int Song;
        public int Count;
        public byte LogCount;
        public int Prediction;
        public float LogPrediction
        {
            get 
            {
                return _logPrediction;
            }
            set 
            {
                _logPrediction = value;
                Prediction = (int)Math.Pow(2, value - 1);
            }
        }
        private float _logPrediction;

        public Info(int user, int song, int count)
        {
            User = user;
            Song = song;
            Count = count;
            LogCount = (byte)Math.Round(Math.Log(count, 2) + 1);
        }
    }
}
