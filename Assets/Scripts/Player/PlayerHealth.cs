using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private int maxHealth = 8;

    private int _currentHealth;
    private Image[] hearts;

    void Start()
    {
        _currentHealth = maxHealth;
        GenerateHearts();
        UpdateHearts();
    }

    private void GenerateHearts()
    {
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }

        int heartCount = Mathf.CeilToInt(maxHealth / 2f);
        hearts = new Image[heartCount];

        for (int i = 0; i < heartCount; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            hearts[i] = heart.GetComponent<Image>();
        }
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        UpdateHearts();
    }

    public void Heal(int amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, maxHealth);
        UpdateHearts();
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            int heartValue = Mathf.Clamp(_currentHealth - (i * 2), 0, 2);

            if (heartValue == 2)
            {
                hearts[i].sprite = fullHeartSprite;
                hearts[i].enabled = true;
            }
            else if (heartValue == 1)
            {
                hearts[i].sprite = halfHeartSprite;
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }
    }
}