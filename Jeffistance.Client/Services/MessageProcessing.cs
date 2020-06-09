using System;
using System.Reflection;
using Jeffistance.Common.Services.MessageProcessing;
using Jeffistance.Client.Models;
using ModusOperandi.Messaging;
using Jeffistance.Client.ViewModels;
using Avalonia.Threading;
using System.Collections.Generic;
using Jeffistance.Common.Models;
using Jeffistance.Common.Services.IoC;
using Jeffistance.Common.Services;

namespace Jeffistance.Client.Services.MessageProcessing
{
    class ClientMessageProcessor : JeffistanceMessageProcessor
    {
        public override void ProcessMessage(Message message)
        {
            base.ProcessMessage(message);
        }
        
        [MessageMethod(JeffistanceFlags.Update)]
        private void UpdateFlagMethod(Message message)
        {
            var appState = AppState.GetAppState();
            while(message.TryPop(out object result))
            {
                (object obj, string name) = (ValueTuple<object, string>) result;
                PropertyInfo pi = appState.GetType().GetProperty(name);
                pi?.SetValue(appState, obj);
            }

        }

        [MessageMethod(JeffistanceFlags.LobbyReady)]
        private void LobbyReadyFlagMethod(Message message)
        {
            AppState.GetAppState().CurrentLobby.AddReadyUser(Guid.Parse((string) message["UserID"]));
        }

        [MessageMethod(JeffistanceFlags.Chat)]
        private void ChatFlagMessage(Message message)
        {
            AppState appState = AppState.GetAppState();
            
            string username = message.TryPop(out object userId, "UserID")?
            appState.GetUserByID((string) userId).Name : null;
            string MessageID = message.TryPop(out object msgId, "MessageID")?
            (string)msgId: null;
            appState.Log(message.Text, username, MessageID);
        }

        [MessageMethod(JeffistanceFlags.EditChatMessage)]
        private void EditChatMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            string msgId = (string) message["MessageID"];
            string newText = (string) message["NewText"];
            appState.EditChatMessage(msgId, newText);
        }
        
        [MessageMethod(JeffistanceFlags.DeleteChatMessage)]
        private void DeleteChatMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            string msgId = (string) message["MessageID"];
            appState.DeleteChatMessage(msgId);
        }

        [MessageMethod(JeffistanceFlags.JoinGameMessage)]
        private void JoinGameMessageFlagMethod(Message message)
        {
            LobbyViewModel lobby = AppState.GetAppState().CurrentLobby;
            Dispatcher.UIThread.Post(()=> lobby.MoveToGameScreen());
            Dispatcher.UIThread.Post(()=> lobby.SetupGame());
        }

        [MessageMethod(JeffistanceFlags.GetPlayerInfoMessage)]
        private void GetPlayerInfoMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            List<Player> players = (List<Player>) message["Players"];
            Player me = players.Find(x => x.UserID == appState.CurrentUser.ID.ToString());
            (appState.CurrentWindow as GameScreenViewModel).RoundBox = "You are in the " + me.Faction.Name + " team";
        }

        [MessageMethod(JeffistanceFlags.GamePhaseReadyMessage)]
        private void GamePhaseReadyMessageFlagMethod(Message message)
        {
            (AppState.GetAppState().CurrentWindow as GameScreenViewModel)
            .AddReadyUser(Guid.Parse((string) message["UserID"]));
        }
        
        [MessageMethod(JeffistanceFlags.AdvanceGamePhaseMessage)]
        private void AdvanceGamePhaseMessageFlagMethod(Message message)
        {
            if(AppState.GetAppState().CurrentUser.IsHost)
            {
                GameScreenViewModel gameScreen = (AppState.GetAppState().CurrentWindow as GameScreenViewModel);
                if(gameScreen.CurrentPhase == Phase.Standby)
                {
                    Game game = AppState.GetAppState().Server.Game;
                    int teamSize = game.NextTeamSize;
                    string leaderID = game.CurrentLeaderID;
                    gameScreen.DeclareLeader(teamSize, leaderID);
                }
                else if(gameScreen.CurrentPhase == Phase.ShowingTeamVoteResult)
                {
                    gameScreen.StartMissionVoting();
                }
                
            }
            
        }

        [MessageMethod(JeffistanceFlags.PickTeamMessage)]
        private void PickTeamMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            gameScreen.TeamPickedUsersIDs = (List<string>) message["PlayersInTeamIDs"];
            string leaderName = appState.UserList.Find(u => u.ID.ToString() == gameScreen.CurrentLeaderID).Name;
            Dispatcher.UIThread.Post(()=>gameScreen.ShowSelectedPlayers());
            gameScreen.RoundBox = leaderName + " picked the following team. It's voting time";
            gameScreen.CurrentPhase = Phase.TeamVoting;
            Dispatcher.UIThread.Post(()=> gameScreen.ChangeOKBtnState(false));
            Dispatcher.UIThread.Post(()=> gameScreen.ChangeVotingBtnsState(true));
            User me = appState.CurrentUser;
            if(me.ID.ToString().Equals(gameScreen.CurrentLeaderID))
            {
                gameScreen.SelectablePlayers = 0;
            }
        }
        
        [MessageMethod(JeffistanceFlags.DeclareLeaderMessage)]
        private void DeclareLeaderMessageFlagMethod(Message message)
        {
            string leaderID =  (string) message["UserID"];
            int teamSize =  (int) message["TeamSize"];
            AppState appState = AppState.GetAppState();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            gameScreen.CurrentLeaderID = leaderID;
            User me = appState.CurrentUser;
            if(me.ID.ToString().Equals(leaderID))
            {
                gameScreen.SelectablePlayers = teamSize;
                Dispatcher.UIThread.Post(()=> gameScreen.ChangeOKBtnState(true));
                gameScreen.RoundBox = "Pick a team of " + teamSize + " players for the next mission";
                gameScreen.CurrentPhase = Phase.TeamPicking;
                
            }
            else
            {
                string leaderName = appState.UserList.Find(u => u.ID.ToString()==leaderID).Name;
                Dispatcher.UIThread.Post(()=> gameScreen.ChangeOKBtnState(false));
                gameScreen.RoundBox = 
                leaderName + " is picking a team of " + teamSize + " for the next mission";
            }
        }

        [MessageMethod(JeffistanceFlags.VoteMessage)]
        private void VoteMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            if(appState.CurrentUser.IsHost)
            {
                string userID = (string) message["UserID"];
                bool vote = (bool) message["Vote"];
                GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
                if(!gameScreen.GameState.TeamVote.ContainsKey(userID))
                {
                    gameScreen.GameState.TeamVote.Add(userID, vote);
                }
                else
                {
                    gameScreen.GameState.TeamVote[userID] = vote;
                }
                if(gameScreen.GameState.TeamVote.Count == appState.UserList.Count) //If everyone voted
                {
                    int yes = 0;
                    int no = 0;
                    foreach(bool b in gameScreen.GameState.TeamVote.Values)
                    {
                        if(b)
                        {
                            yes++;
                        }
                        else
                        {
                            no++;
                        }
                    }
                    //Finish voting phase
                    if(yes>no)
                    {
                        Dictionary<string, bool> voters = gameScreen.GameState.TeamVote;
                        gameScreen.GameState.TeamVote = new Dictionary<string, bool>();
                        Dispatcher.UIThread.Post(()=> gameScreen.MakeShowVotingResultMessage(voters));
                    }
                    else
                    {
                        if(appState.Server.Game.FailedVoteCount == appState.Server.Game.MaxFailedVotes)
                        {
                            //random and to next turn
                        }
                        else
                        {
                            appState.Server.Game.FailedVoteCount++;
                            gameScreen.GameState.TeamVote = new Dictionary<string, bool>();
                            //change leader
                            //repeat team voting phase
                        }
                    }
                }
            }
        }

        [MessageMethod(JeffistanceFlags.StartMissionVotingMessage)]
        private void StartMissionVotingMessageFlagMethod(Message message)
        {
            List<string> PlayersInTeamIDs = (List<string>) message["PlayersInTeamIDs"];
            AppState appState = AppState.GetAppState();
            string myID = appState.CurrentUser.ID.ToString();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            if(PlayersInTeamIDs.Contains(myID))
            {
                gameScreen.RoundBox = "Decide on the mission's success";
                Dispatcher.UIThread.Post(()=> gameScreen.ShowMissionVotingInterface());
                gameScreen.CurrentPhase = Phase.MissionVoting;
            }
            else
            {
                gameScreen.RoundBox = "The team is executing the mission";
                gameScreen.CurrentPhase = Phase.MissionVoting;
            }
            
        }

        [MessageMethod(JeffistanceFlags.ShowTeamVoteResultMessage)]
        private void ShowTeamVoteResultMessageFlagMethod(Message message)
        {
            Dictionary<string, bool> voters = (Dictionary<string, bool>) message["Voters"];
            AppState appState = AppState.GetAppState();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            Dispatcher.UIThread.Post(()=> gameScreen.ShowTeamVoteResult(voters));
        }

        [MessageMethod(JeffistanceFlags.MissionVoteMessage)]
        private void MissionVoteMessageFlagMethod(Message message) //need the list of the mission team
        {
            AppState appState = AppState.GetAppState();
            if(appState.CurrentUser.IsHost)
            {
                string userID = (string) message["UserID"];
                bool vote = (bool) message["Vote"];
                GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
                if(gameScreen.TeamPickedUsersIDs.Contains(userID)) //You can only vote if you are in the team
                {
                    if(!gameScreen.GameState.TeamVote.ContainsKey(userID))
                    {
                        gameScreen.GameState.TeamVote.Add(userID, vote);
                    }
                    else
                    {
                        gameScreen.GameState.TeamVote[userID] = vote;
                    }
                }
                if(gameScreen.GameState.TeamVote.Count == gameScreen.TeamPickedUsersIDs.Count) //If everyone IN TEAM voted
                {
                    bool missionSucceeds = true;
                    foreach(bool b in gameScreen.GameState.TeamVote.Values)
                    {
                        if(!b)
                        {
                            missionSucceeds = false;
                        }
                    }
                    gameScreen.GameState.TeamVote = new Dictionary<string, bool>();
                    gameScreen.ReadyUserIDs = new List<Guid>();
                    AppState apps = AppState.GetAppState();
                    Game game = apps.Server.Game;
                    if(missionSucceeds)
                    {
                        game.ResistanceWinCount++;
                    }
                    else
                    {
                        game.SpiesWinCount++;
                    }
                    game.NextRound();
                    var user = apps.CurrentUser;
                    var messageFactory = IoCManager.Resolve<IClientMessageFactory>();
                    var voteMessage = messageFactory.MakeShowMissionResultMessage(missionSucceeds);
                    user.Send(voteMessage);
                }
            }
        }

        [MessageMethod(JeffistanceFlags.ShowMissionResultMessage)]
        private void ShowMissionResultMessageFlagMethod(Message message)
        {
            bool result = (bool) message["Result"];
            AppState appState = AppState.GetAppState();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            Dispatcher.UIThread.Post(()=> gameScreen.ResolveMissionResult(result));
            Dispatcher.UIThread.Post(()=> gameScreen.RestorePlayersToNormal());
            gameScreen.TeamPickedUsersIDs = new List<string>();
            AppState apps = AppState.GetAppState();
            if(apps.CurrentUser.IsHost)
            {
                Game game = apps.Server.Game;
                if(game.CurrentPhase == Phase.GameEnd) //if game ended
                {
                    IFaction winner = game.Winner;
                    List<string> spyPlayersIDs = new List<string>();
                    foreach(Player p in game.Players)
                    {
                        if(p.Faction is SpiesFaction)
                        {
                            spyPlayersIDs.Add(p.UserID);
                        }
                    }
                    var user = apps.CurrentUser;
                    var messageFactory = IoCManager.Resolve<IClientMessageFactory>();
                    var endGameMessage = messageFactory.MakeEndGameMessage(winner.Name, spyPlayersIDs); 
                    user.Send(endGameMessage);
                }
            }
        }
        
        [MessageMethod(JeffistanceFlags.EndGameMessage)]
        private void EndGameMessageFlagMethod(Message message)
        {
            string winningFactionName = (string) message["Name"];
            List<string> spiesIDs = (List<string>) message["SpyPlayersIDs"];
            AppState appState = AppState.GetAppState();
            GameScreenViewModel gameScreen = (appState.CurrentWindow as GameScreenViewModel);
            Dispatcher.UIThread.Post(()=> gameScreen.ShowEndGameResults(winningFactionName, spiesIDs));
        }
        
    }
}
