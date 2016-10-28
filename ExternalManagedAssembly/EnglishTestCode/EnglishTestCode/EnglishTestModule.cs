﻿using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnglishTestModule : MonoBehaviour
{
    public Shader UnlitShader;

    private KMBombModule module;
    private GameObject topDisplay;
    private TextMesh topText;
    private GameObject bottomDisplay;
    private TextMesh bottomText;
    private TextMesh optionsText;

    private bool activated;
    private List<Question> questions;
    private int solvedQuestions;
    private int targetQuestions;
    private Question currentQuestion;
    private int currentAnswerIndex;

    public void Start()
    {
        activated = false;
        solvedQuestions = 0;
        targetQuestions = 3;

        Module.OnActivate += OnActivate;
        findChildGameObjectByName(gameObject, "Submit Button").GetComponent<KMSelectable>().OnInteract += OnSubmitInteract;
        findChildGameObjectByName(gameObject, "Left Button").GetComponent<KMSelectable>().OnInteract += OnLeftInteract;
        findChildGameObjectByName(gameObject, "Right Button").GetComponent<KMSelectable>().OnInteract += OnRightInteract;

        questions = new List<Question>();
        string[] lines = EnglishTestCode.Properties.Resources.setnences.Split('\n', '\r');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
                continue;
            Regex regex = new Regex(@"(?<=\[).*(?=\])");
            Match match = regex.Match(line);
            if (match.Success)
            {
                Question question = new Question();
                question.QuestionText = line.Replace(match.Value, "%");
                string[] answers = match.Value.Split('|');
                for (int j = 0; j < answers.Length; j++)
                {
                    string answer = answers[j];
                    if (answer.StartsWith("!"))
                    {
                        question.CorrectAnswerIndex = j;
                        answer = answer.Substring(1);
                    }
                    question.Answers.Add(answer);
                }
                questions.Add(question);
            }
            else
            {
                Debug.Log("Couldn't find options match for string at line " + i);
            }
        }

#if !UNITY_EDITOR
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Submit Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Left Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Right Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
#endif
    }

    private void OnActivate()
    {
        activated = true;

        selectQuestion();
    }

    private bool OnSubmitInteract()
    {
        if (!activated)
            return false;

        selectQuestion();
        return false;
    }

    private bool OnLeftInteract()
    {
        if (!activated)
            return false;

        currentAnswerIndex--;
        if (currentAnswerIndex < 0)
            currentAnswerIndex = currentQuestion.Answers.Count - 1;
        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[currentAnswerIndex] + "</i>"));
        updateOptionsText();
        return false;
    }

    private bool OnRightInteract()
    {
        if (!activated)
            return false;

        currentAnswerIndex++;
        if (currentAnswerIndex >= currentQuestion.Answers.Count)
            currentAnswerIndex = 0;
        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[currentAnswerIndex] + "</i>"));
        updateOptionsText();
        return false;
    }

    private void selectQuestion()
    {
        currentQuestion = questions[UnityEngine.Random.Range(0, questions.Count)];
        currentAnswerIndex = 0;

        TopText.text = "Question " + (solvedQuestions + 1) + "/" + targetQuestions;
        TopText.gameObject.SetActive(true);

        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[currentAnswerIndex] + "</i>"));
        BottomText.gameObject.SetActive(true);

        updateOptionsText();
        OptionsText.gameObject.SetActive(true);
    }

    private void setBottomText(string text)
    {
        float maxX = BottomDisplay.GetComponent<BoxCollider>().size.x * BottomDisplay.transform.localScale.x - 0.007f;
        float maxZ = BottomDisplay.GetComponent<BoxCollider>().size.z * BottomDisplay.transform.localScale.z;
        BottomText.fontSize = 35;
        BottomText.text = text;
        string originalText = BottomText.text;
        while (getGameObjectSize(BottomText.gameObject).x > maxX)
        {
            string currentText = BottomText.text;
            List<int> indices = findAllIndicesOf(currentText, ' ');
            int i = indices.Count - 1;
            if (i >= 0)
            {
                while (getGameObjectSize(BottomText.gameObject).x > maxX)
                {
                    BottomText.text = currentText.Substring(0, indices[i]);
                    i--;
                    if (i < 0)
                        break;
                }
            }
            if (i >= 0)
            {
                BottomText.text += "\n" + currentText.Substring(indices[i + 1] + 1);
            }
            else
            {
                BottomText.fontSize--;
            }
            if (findAllIndicesOf(BottomText.text, '\n').Count > 3)
            {
                BottomText.fontSize--;
                BottomText.text = originalText;
            }
        }
    }

    private void updateOptionsText()
    {
        OptionsText.fontSize = 35;
        float wordWidth = 0;
        float wordBegin = 0;
        while (true)
        {
            string str = "";
            for (int i = 0; i < currentQuestion.Answers.Count; i++)
            {
                if (i > 0)
                    str += " ";

                if (i == currentAnswerIndex)
                {
                    OptionsText.text = currentQuestion.Answers[i];
                    wordWidth = getGameObjectSize(OptionsText.gameObject).x;
                    OptionsText.text = str;
                    wordBegin = getGameObjectSize(OptionsText.gameObject).x;
                    str += "<color=#000000>" + currentQuestion.Answers[i] + "</color>";
                }
                else
                {
                    str += currentQuestion.Answers[i];
                }
            }
            OptionsText.text = str;
            if (getGameObjectSize(OptionsText.gameObject).x < BottomDisplay.GetComponent<BoxCollider>().size.x * BottomDisplay.transform.localScale.x)
                break;
            OptionsText.fontSize--;
        }

        if (OptionsText.transform.childCount > 0)
            Destroy(OptionsText.transform.GetChild(0).gameObject);
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Plane);
        background.name = "Highlight plane";
        background.transform.parent = OptionsText.gameObject.transform;
        background.transform.localPosition = new Vector3(wordBegin - (getGameObjectSize(OptionsText.gameObject).x / 2) + wordWidth / 2, 0, 0.000001f);
        background.transform.localScale = new Vector3(wordWidth / 10, 1, getGameObjectSize(OptionsText.gameObject).y / 10);
        background.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));
        background.GetComponent<Renderer>().materials[0].color = Color.green;
    }

    private Vector3 getGameObjectSize(GameObject go)
    {
        BoxCollider col = go.AddComponent<BoxCollider>();
        Vector3 size = col.size;
        Destroy(col);
        return size;
    }

    private List<int> findAllIndicesOf(string str, char chr)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == chr)
                indices.Add(i);
        }
        return indices;
    }

    private GameObject findChildGameObjectByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
                return child.gameObject;
            GameObject childGo = findChildGameObjectByName(child.gameObject, name);
            if (childGo != null)
                return childGo;
        }
        return null;
    }

    public KMBombModule Module
    {
        get
        {
            if (module == null)
                module = GetComponent<KMBombModule>();
            return module;
        }
    }

    public GameObject TopDisplay
    {
        get
        {
            if (topDisplay == null)
                topDisplay = findChildGameObjectByName(gameObject, "Top Display");
            return topDisplay;
        }
    }

    public TextMesh TopText
    {
        get
        {
            if (topText == null)
                topText = findChildGameObjectByName(gameObject, "Top Text").GetComponent<TextMesh>();
            return topText;
        }
    }

    public GameObject BottomDisplay
    {
        get
        {
            if (bottomDisplay == null)
                bottomDisplay = findChildGameObjectByName(gameObject, "Bottom Display");
            return bottomDisplay;
        }
    }

    public TextMesh BottomText
    {
        get
        {
            if (bottomText == null)
                bottomText = findChildGameObjectByName(gameObject, "Bottom Text").GetComponent<TextMesh>();
            return bottomText;
        }
    }

    public TextMesh OptionsText
    {
        get
        {
            if (optionsText == null)
                optionsText = findChildGameObjectByName(gameObject, "Options Text").GetComponent<TextMesh>();
            return optionsText;
        }
    }
}
