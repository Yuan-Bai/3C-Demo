using System.Collections.Generic;

public sealed class CharacterCommandBuffer
{
    private readonly Dictionary<CommandChannel, CharacterCommand> _slots = new();

    public void Push(CharacterCommand command, float now)
    {
        if (command.ExpiresAt <= now) return;

        if (_slots.TryGetValue(command.Channel, out var old))
        {
            if (old.Priority > command.Priority) return;
        }

        _slots[command.Channel] = command;
    }

    public bool TryConsume(CommandChannel channel, float now, out CharacterCommand command)
    {
        if (_slots.TryGetValue(channel, out command) && command.ExpiresAt > now)
        {
            _slots.Remove(channel);
            return true;
        }

        command = default;
        _slots.Remove(channel);
        return false;
    }
}