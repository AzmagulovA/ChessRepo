using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using ChessEngine.Exceptions;
using ChessEngine.Models;
using ChessEngine.NET;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChessEngine.Core
{
    public class Stockfish : IStockfish
    {
        #region private variables

        /// <summary>
        /// 
        /// </summary>
        private const int MAX_TRIES = 1000;

        /// <summary>
        /// 
        /// </summary>
        private int _skillLevel;

        #endregion

        # region private properties

        /// <summary>
        /// 
        /// </summary>
        private StockfishProcess _stockfish { get; set; }

        #endregion

        #region public properties

        /// <summary>
        /// 
        /// </summary>
        public Settings Settings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int SkillLevel
        {
            get => _skillLevel;
            set
            {
                _skillLevel = value;
                Settings.SkillLevel = SkillLevel;
                setOption("Skill level", SkillLevel.ToString());
            }
        }

        public MaxTriesException MaxTriesException
        {
            get => default;
            set
            {
            }
        }

        #endregion

        #region constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="depth"></param>
        /// <param name="settings"></param>
        public Stockfish(
            string path,
            int depth = 12,
            Settings settings = null)
        {
            Depth = depth;
            _stockfish = new StockfishProcess(path);
            _stockfish.Start();
            _stockfish.ReadLine();

            if (settings == null)
            {
                Settings = new Settings();
            }
            else
            {
                Settings = settings;
            }

            SkillLevel = Settings.SkillLevel;
            foreach (var property in Settings.GetPropertiesAsDictionary())
            {
                setOption(property.Key, property.Value);
            }

            startNewGame();
        }

        #endregion

        #region private

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="estimatedTime"></param>
        private void send(string command, int estimatedTime = 100)
        {
            _stockfish.WriteLine(command);
            _stockfish.Wait(estimatedTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MaxTriesException"></exception>
		private bool isReady()
        {
			send("isready");
			var tries = 0;
			while (tries < MAX_TRIES) {
				++tries;

				if (_stockfish.ReadLine() == "readyok") {
					return true;
				}
			}
			throw new MaxTriesException();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <exception cref="ApplicationException"></exception>
        private void setOption(string name, string value)
        {
            send($"setoption name {name} value {value}");
            if (!isReady())
            {
                throw new ApplicationException();
            }
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        private void startNewGame()
        {
            send("ucinewgame");
            if (!isReady())
            {
                throw new ApplicationException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void go()
        {
            send($"go depth {Depth}");
        }

       

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<string> readLineAsList()
        {
            var data = _stockfish.ReadLine();
            
            return data.Split(' ').ToList();
            
            
        }

        #endregion

        #region public


        
      
        /// <summary>
        /// Set position in fen format
        /// </summary>
        /// <param name="fenPosition"></param>
        public void SetFenPosition(string fenPosition)
        {
            startNewGame();
            send($"position fen {fenPosition}");
        }
 
        public List<List<string>> GetBestMoves(string FEN)
        {
            SetFenPosition(FEN);
            go();//подсчет на глубину в 10 ходов
            var tries = 0;
            List<List<string>> Text = new List<List<string>>();
            while (true)
            {
                var data = readLineAsList();
                string eval = "";
                string type;
                int digit;

                if (data[0] == "bestmove")
                {
                    return Text;
                }

                if ((data.Count > 9) && (int.TryParse(data[2], out digit)) && (int.Parse(data[2]) == Depth))//если строка с найденной глубиной
                {
                    type = data[8];
                    eval = eval + data[9];//eval
                    data.RemoveRange(0, 21);//получение только ходов
                    data.Insert(0, eval);
                    data.Insert(0, type);

                    Text.Add(data);

                }
                tries++;
            }
        }

       
        



        #endregion
    }
}
