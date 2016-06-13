using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NetUtilities;
using LogUtilities;

namespace PokerServer
{
    public class GameRoom
    {
        #region Fields

        public const int MAX_SEAT = 10;
        public const int BUY_IN = 100;

        private Dictionary<int, GamePlayer> seat = new Dictionary<int,GamePlayer>();
        private Dictionary<GamePlayer, int> seatReverse = new Dictionary<GamePlayer, int>();
        private Dictionary<string, GamePlayer> spectators = new Dictionary<string, GamePlayer>();

        private ManualResetEvent denyListAccess = new ManualResetEvent(true);

        private string name;

        #endregion

        #region Properties

        public string Name
        {
            get { return name; }
        }

        public bool Joinable
        {
            get
            {
                return PlayerCount < MAX_SEAT;
            }
        }

        public int PlayerCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < MAX_SEAT; i++)
                {
                    if (seat.ContainsKey(i))
                        cnt++;
                }
                return cnt;
            }
        }

        public int TotalChips
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < MAX_SEAT; i++)
                {
                    if (seat.ContainsKey(i))
                        cnt += seat[i].Wallet;
                }
                return cnt;
            }
        }

        public int BuyIn
        {
            get { return BUY_IN; }
        }

        #endregion

        public GameRoom(string name)
        {
            this.name = name;
        }

        #region Events and Delegates

        #endregion

        #region Event Handlers

        public void OnQuitEvent(GamePlayer player)
        {
            if (seat.ContainsValue(player))
            {
                List<int> toRemoveA = new List<int>();
                List<GamePlayer> toRemoveB = new List<GamePlayer>();

                foreach (KeyValuePair<int, GamePlayer> i in seat)
                {
                    if (i.Value.Name == player.Name)
                    {
                        toRemoveA.Add(i.Key);
                        toRemoveB.Add(i.Value);
                    }
                }

                foreach (int i in toRemoveA)
                {
                    seat.Remove(i);
                }

                foreach (GamePlayer i in toRemoveB)
                {
                    seatReverse.Remove(i);
                }
            }

            if (spectators.ContainsKey(player.Name))
            {
                spectators.Remove(player.Name);
            }

            //player.Destroy();
        }

        public void OnChatEvent(string name, string msg)
        {
            Chat(name, msg);
        }

        public bool OnAttemptSitEvent(GamePlayer player, int seatnum)
        {
            return JoinGame(player, seatnum);
        }

        public void OnStandEvent(GamePlayer player)
        {
            if (seatReverse.ContainsKey(player))
            {
                int i = seatReverse[player];
                seat.Remove(i);
                seatReverse.Remove(player);

                Logger.Log(LogType.Event, player.Name + " stood up");

                BroadCastSeatInfo(i);
            }
            else
            {
                Logger.Log(LogType.Error, player.Name + " tried to stand up when he wasn't sitting");
            }
        }

        #endregion

        #region Private Methods

        private bool JoinGame(GamePlayer player, int seatnum)
        {
            if (seat.ContainsKey(seatnum))
            {
                Logger.Log(LogType.Error, player.Name + " tried to take an occupied seat");
            }
            else
            {
                if (seatnum >= 0 && seatnum < MAX_SEAT)
                {
                    if (seat.ContainsValue(player) == false)
                    {
                        seat.Add(seatnum, player);
                        seatReverse.Add(player, seatnum);
                        player.Tunnel.SendMessage("ok");
                        BroadCastSeatInfo(seatnum);
                        return true;
                    }
                    else
                    {
                        Logger.Log(LogType.Error, player.Name + " tried to sit without standing up first");
                    }
                }
                else
                {
                    Logger.Log(LogType.Error, player.Name + " tried to take a non-existant seat");
                }
            }

            return false;
        }

        public void BroadcastSeatInfo(GamePlayer player)
        {
            if (seatReverse.ContainsKey(player))
                BroadCastSeatInfo(seatReverse[player]);
        }

        private void BroadCastSeatInfo(int seatnum)
        {
            List<GamePlayer> tempList = new List<GamePlayer>(spectators.Values);
            foreach (GamePlayer gp in tempList)
            {
                SendPlayerInfo(gp, seatnum);
            }
        }

        private void SendPlayerInfo(GamePlayer recipient, int seatnum)
        {
            if (seat.ContainsKey(seatnum))
            {
                recipient.Tunnel.SendMessage(String.Format(
                    "seatinfo:{0},{1},{2}",
                    seatnum, seat[seatnum].Name, seat[seatnum].Wallet
                    )
                );
            }
            else
            {
                recipient.Tunnel.SendMessage(String.Format(
                    "seatinfo:{0},{1},{2}",
                    seatnum, "", 0
                    )
                );
            }
        }

        private void SendAllPlayerInfo(GamePlayer recipient)
        {
            for (int i = 0; i < MAX_SEAT; i++)
            {
                SendPlayerInfo(recipient, i);
            }
        }

        #endregion

        #region Public Methods

        public void Chat(string name, string msg)
        {
            List<GamePlayer> tempList = new List<GamePlayer>(spectators.Values);
            foreach (GamePlayer gp in tempList)
            {
                gp.Chat(name, msg);
            }
        }

        public bool ContainsPlayer(string name)
        {
            if (spectators.ContainsKey(name))
            {
                if (spectators[name].Tunnel.Ping())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool WatchGame(GamePlayer player)
        {
            if (spectators.ContainsKey(player.Name))
            {
                if (spectators[player.Name].Tunnel.SendMessage("alreadyin"))
                {
                    Logger.Log(LogType.Error, player.Name + " tried to join a game that he is already in");
                    return false;
                }
                else
                {
                    Logger.Log(LogType.Error, player.Name + " is rejoining a game he never properly quit from");
                    spectators.Remove(player.Name);
                }
            }

            player.Tunnel.SendMessage(player.Name);
            player.Tunnel.SendMessage(name);

            spectators.Add(player.Name, player);
            SendAllPlayerInfo(player);

            player.SetBank(player.Bank);

            return true;
        }

        public void Destroy()
        {
            Logger.Log(LogType.Event, "GameRoom being destroyed");
        }

        #endregion
    }
}
