using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    private int choice;

    public float Money = 5;
    public float max = 100;
    public float min = 5;


    public List<Button> Choices;

    public TMP_Text myText;

    public Button Plus, Minus;

    public Button PlaceBet;

    private Dictionary<Button,int> myDico;

    public Sprite Selected;

    public Sprite Unselected;



    void Start()
    {
        myText.text = Money.ToString();
        if (Money <= min) Minus.interactable = false;
        if (Money >= max) Plus.interactable = false;
        PlaceBet.interactable = false;

        myDico = new Dictionary<Button, int>();
        for (int i = 0; i < Choices.Count; i++)
        {
            myDico[Choices[i]] = i;
        }

    }

    public void add()
    {
        Money += 5;
        myText.text = Money.ToString();

        if (Money >= max) Plus.interactable = false; 
        if (Money >= min) Minus.interactable = true;
    }

    public void sub()
    {
        Money -= 5;
        myText.text = Money.ToString();

        if (Money <= max) Plus.interactable = true;
        if (Money <= min) Minus.interactable = false;
    }

    public void makeAChoice(Button myChoice)
    {
        Debug.Log("PlaceBet interactable: " + PlaceBet.interactable);

        choice = myDico[myChoice];
        foreach (Button button in Choices)
        {
            if (button == myChoice) { button.image.sprite = Selected; }
            else { button.image.sprite = Unselected; }
        }

        PlaceBet.interactable = true;

        Debug.Log("PlaceBet interactable: " + PlaceBet.interactable);

    }

    public void BetPlaced()
    {
        Debug.Log("Bet placed on " + choice.ToString() + " : " + Money.ToString());
    }
}
