using UnityEngine;

public interface INotModelForwardTarget
{
    // MAINLY USED FOR PROJECTILES, TRAPS, AND OTHER SELF DESTRUCTING HITBOXES.

    // When knocked back, the player's model forward for the PlayerAnimation will be the attacker's transform.
    // BUT, with self destructing hitboxes like projectiles or traps and things like that, we don't want that
    // because it's not a constant target, it gets destroyed which leads error, etc..
    // So this interface is for those hitboxes that don't want the model forward to be the attacker's transform.
}
