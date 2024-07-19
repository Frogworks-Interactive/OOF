﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OofPlugin
{
    public class DeadPlayersList
    {
        public class DeadPlayer
        {
            public uint PlayerId;
            public bool DidPlayOof { get; set; } = false;
            public Vector3 Distance = Vector3.Zero;
        }
        public List<DeadPlayer> DeadPlayers { get; set; } = new List<DeadPlayer>();

        /// <summary>
        /// Handle Player death, and add distance if true
        /// </summary>
        /// <param name="character">character </param>
        /// <param name="condition">extra condition</param>
        private void AddRemoveDeadPlayer(DeadPlayer deadPlayer, uint currentHp, uint objectId, bool condition = true)
        {
            if (currentHp == 0 && !DeadPlayers.Any(x => x.PlayerId == objectId) && condition)
            {
                DeadPlayers.Add(new DeadPlayer { PlayerId = objectId });
            }
            else if (currentHp != 0 && DeadPlayers.Any(x => x.PlayerId == objectId))
            {
                DeadPlayers.RemoveAll(x => x.PlayerId == objectId);

            }
        }
        /// <summary>
        /// add remove player for IPlayer Character
        /// </summary>
        /// <param name="character"></param>
        public void AddRemoveDeadPlayer(IPlayerCharacter character)
        {
            if (character == null) return;

            var deadPlayer = new DeadPlayer { PlayerId = character.DataId };
            AddRemoveDeadPlayer(deadPlayer, character.CurrentHp, character.DataId);
        }

        /// <summary>
        /// add remove player for Party/alliance members
        /// </summary>
        /// <param name="character"></param>
        /// <param name="condition"></param>
        public void AddRemoveDeadPlayer(IPartyMember character, bool condition = true)
        {
            if (character == null) return;
            var deadPlayer = new DeadPlayer { PlayerId = character.ObjectId, Distance = character.Position };
            AddRemoveDeadPlayer(deadPlayer, character.CurrentHP, character.ObjectId, condition);
        }

    }
}
