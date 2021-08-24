const { v4: uuidv4 } = require('uuid');
const http = require('http');
const httpServer = http.createServer();
const socket = require('socket.io');
const { join } = require('path');
const { Console } = require('console');

var io = socket(httpServer, {
  pingInterval: 10000,
  pingTimeout: 5000
});

var port = process.env.PORT || 3000,



// socketToLobbyPlayer[socket.id] = {player : .., lobbyId : ..}
socketToLobbyPlayer = {};

lobbies = [];
lobbyLevelDict = {};


io.on('connection', function(socket){
    console.log('socket connected: ' + socket.id);

    socket.on("open", () => {
      console.log(`open`);

    });

    socket.on("error", e => {
      console.log(e);
    })

    socket.on("connect", () => {
      console.log(`connect`);

    });

    socket.on('disconnect', function(){
      console.log(`disconnect`);
      removeDisconnectedSocketFromLobby(socket);
    });

    socket.on("CreateLobby", (lobby, player) => {

      //Handle cases where lobby already exists
      let exists = lobbies.find(e => e.Id === lobby.Id);
      if (exists) {
        console.log(`${player.Name} tried to create lobby ${lobby.Id} which already exists`);
        socket.emit("CreateLobbyError", {Call: "CreateLobby", Reason: "Lobby Already Exists"});
        return;
      }

      lobby.Host = player;
      if (!lobby.Players)
        lobby.Players = []
      lobby.Players.push(player);
      lobbies.push(lobby);
      socket.join(lobby.Id);
      console.log(`${player.Name} Created lobby with name ${lobby.Id}`);
      console.log(lobby);

      socket.emit("CreatedLobby", lobby);
      addSocketToLobby(lobby, player, socket);
    });

    
    socket.on("JoinLobby", (player, lobbyId) => {
      let lobby = lobbies.find(e => e.Id === lobbyId);
      //check lobby actually exists
      if (lobby)
      {
        //Handle lobby being full
        if (lobby.Players.length >= lobby.MaxPlayerCount) {
          socket.emit("LobbyFull");
          return;
        }

        //Player joined lobby
        lobby.Players.push(player);
        socket.join(lobbyId);
        socket.to(lobbyId).emit("PlayerJoinedLobby", player);
        socket.emit("JoinedLobby", lobby);
        //socket.emit("LevelSelected", lobbyLevelDict[lobby.Id]);
        console.log(`Player ${player.Name} joined ${lobbyId}`);
        addSocketToLobby(lobby, player, socket);
      }
      else
      {
        console.log(`${player.Name} tried to join ${lobbyId}`)
        socket.emit("JoinLobbyError", {Call: "JoinLobby", Reason: "Failed lobby does not exists"});
      }

    });

    socket.on("LeaveLobby", (player, lobbyId) => {
      let lobby = lobbies.find(e => e.Id === lobbyId)
      socket.leave(lobbyId); //we make sure he is leaving room even if it has been deleted
      if (lobby) {
        io.to(lobby.Id).emit("PlayerLeftLobby", player);
        removeSocketFromLobby(lobby, player, socket)
        console.log(`Player ${player.Name} left ${lobby.Id}`);

        if (lobby.Players.length === 0) {
          deleteLobby(lobby.Id);
        }
      }
    });

    socket.on("DeleteLobby", (lobbyId) => {
      console.log(`Deleting lobby with id: ${lobbyId}`);

      let lobby = lobbies.find(e => e.Id === lobbyId);
      if (lobby) {
        socket.to(lobby.Id).emit("LobbyClosed", lobby);
        removeSocketFromLobby(lobby, lobby.Host, socket);
      }
      
      //We are sending lobby closed event to all players of the lobby
      //thus they call LeaveLobby. Here we delete lobby when 0 players are left
      //deleteLobby(lobbyId);
    });

    socket.on("GetLobbyList", () => {
      console.log("GetLobbyList");
      socket.emit("GetLobbyList", lobbies);
    });

    socket.on("SetLevel", (lobbyId, setLevel) => {
      let lobby = lobbies.find(e => e.Id === lobbyId);
      if (lobby) {
        lobby.Level = setLevel;
        socket.to(lobby.Id).emit("LevelSelected", setLevel);
        console.log(`Lobby selected level in group ${setLevel.GroupName} with index ${setLevel.SongName} for difficulty ${setLevel.Difficulty}`);
      }
      else
      console.log(`failed to set new selected level because lobby with id: ${lobbyId}, does not exist`);
    });
    
    //TODO handle seed data from mods that are random by nature
    socket.on("SetModifiers", (lobbyId, modifiers) => {
      let lobby = lobbies.find(e => e.Id === lobbyId);
      if (lobby) {
        lobby.Modifiers = modifiers;
        socket.to(lobby.Id).emit("SetModifiers", lobby.Modifiers);
        console.log(`Lobby host of ${lobby.Id} selected ${modifiers} as modifiers`);
      }
      else
        console.log(`Host tried to set modifiers for ${lobbyId}, but lobby does not exist`);

    });


    socket.on("StartGame", (lobbyId, startGame) => {
      let lobby = lobbies.find(e => e.Id === lobbyId);
      if (lobby) {
        io.in(lobby.Id).emit("StartGame", startGame);
        console.log(`Lobby ${lobby.Id} started game with delay: ${startGame}`);
      }
      else
       console.log(`failed to start game because lobby with id: ${lobbyId}, does not exist`);
      
    });

    socket.on("ScoreUpdate", (lobbyId, updateScore) => {
      let lobby = lobbies.find(e => e.Id === lobbyId);
      if (lobby) {
        console.log("ScoreUpdate");
        io.in(lobby.Id).emit("OnScoreSync", updateScore);
      }
    });

});

httpServer.listen(port, () => {
  console.log("Listening on *:" + port);
});

function deleteLobby(lobbyId) {
  var index = lobbies.findIndex(e => e.Id === lobbyId);
  if (index >= 0) {
    lobbies.splice( index, 1 );
    console.log(`Deleted lobby ${lobbyId}`);
  }
}

function addSocketToLobby(lobby, player, socket)
{
  socketToLobbyPlayer[socket.id] = {LobbyId: lobby.Id, Player: player};
}

function removeSocketFromLobby(lobby, player, socket)
{
  if (socketToLobbyPlayer[socket.id])
  {
    let index = lobby.Players.findIndex(p => p.Name === player.Name);
    if (index !== -1) 
    {
      lobby.Players.splice(index, 1);
      if (lobby.Players.length === 0) {
        deleteLobby(lobby.Id);
      }
    }
    delete socketToLobbyPlayer[socket.id];
  }
}

//We try to handle cases where a player is joined a lobby but disconnect
//In these cases we want to call the LeaveLobby or DeleteLobby accordingly
function removeDisconnectedSocketFromLobby(socket)
{
  let obj = socketToLobbyPlayer[socket.id];
  if (obj)
  {
    let lobby = lobbies.find(l => l.Id === obj.LobbyId);
    let player = obj.Player;
    if (lobby) 
    {
      //If player happens to be host we have to delete lobby, otherwise we can
      //get by, by just removing the player from the lobby.      
      if (lobby.Host.Name === player.Name) {
        socket.to(lobby.Id).emit("LobbyClosed", lobby);
        console.log(`Socket: ${socket.id} disconnected. He was host, thus deleting lobby: ${lobby.Id}`);
      }
      else {
        io.to(lobby.Id).emit("PlayerLeftLobby", player);
        console.log(`Socket: ${socket.id} disconnected. Removed him from lobby: ${lobby.Id}`);
      }      

      removeSocketFromLobby(lobby, player, socket);
    }
  }
  else
  {
    console.log(`Socket: ${socket.id} was not found to be part of a lobby`);
  }
}

