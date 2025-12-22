using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OofPlugin {
  public class DeadPlayersList {
    public class DeadPlayer {
      public uint PlayerId;
      public bool DidPlayOof { get; set; } = false;
      public Vector3 Distance = Vector3.Zero;
    }

    public List<DeadPlayer> DeadPlayers { get; set; } = new();

    private void AddRemoveDeadPlayer(uint currentHp, uint entityId, Vector3 pos) {
  

      if (currentHp == 0 && !DeadPlayers.Any(x => x.PlayerId == entityId)) {
        DeadPlayers.Add(new DeadPlayer { PlayerId = entityId, Distance = pos });
      }
      else if (currentHp != 0 &&
                 DeadPlayers.Any(x => x.PlayerId == entityId)) {
        DeadPlayers.RemoveAll(x => x.PlayerId == entityId);
      }
    }

    public void AddRemoveDeadPlayer(IPlayerCharacter character) {
      if (character == null)
        return;
      AddRemoveDeadPlayer(character.CurrentHp, character.EntityId,
                          character.Position);
    }

    public void AddRemoveDeadPlayer(IPartyMember character) {
      if (character == null)
        return;
      AddRemoveDeadPlayer(character.CurrentHP, character.EntityId,
                          character.Position);
    }
  }
}
