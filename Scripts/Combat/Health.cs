using System;
using UnityEngine;


// ANYTHING IMPLEMENTING HEALTH MUST IMPLEMENT THIS INTERFACE. Technically, there is other ways of doing this like with an abstract class, but I think this is cleaner and easy to remember. Only adding this comment just in case 
public interface IHasHealth
{
    Health Health { get; }
}

[Serializable]
public class Health
{
    public float maxHealth = 40f;
    public float currentHealth = 40f;
    public bool canTakeDamage = true;

    public event Action<float> OnHealthChange;
    public event Action OnDeath;

    public void TakeDamage(float amount)
    {
        if (!canTakeDamage) return;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        OnHealthChange?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void RestoreToFull()
    {
        currentHealth = maxHealth;
        OnHealthChange?.Invoke(1f);
    }

    public virtual void Die()
    {
        OnDeath?.Invoke();
        // Debug.Log("Entity has died.");
    }
}
