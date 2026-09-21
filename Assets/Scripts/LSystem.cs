using System;
using System.Collections.Generic;
using UnityEngine;


public class LSystem : MonoBehaviour
{
    
    /// <summary>
    ///  Grammar
    /// Axiom
    /// Any Production Rules
    /// Interpreter
    /// </summary>
    
    public GameObject treeBranch;
    public GameObject leafBlob;
    public float leafChance = 0.5f;
    public int depth = 1;
    public string axiom;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string tree = Generate(axiom, depth);
        Debug.Log($"Tree: {tree}");
        Interpret(tree);
    }

 
    [Serializable]
    public class Production
    {
        public string lhs;
        public string rhs;
    }

    public List<Production> productionRules;

    private string Generate(string current, int depth)
    {
        if (depth == 0)
        {
            return current;
        }
        else
        {
            string result = "";
            
            foreach (string token in current.Split(" "))
            {
                if (result != "")
                {
                    result += " ";
                }
                    result += Generate(Replace(token), depth - 1);
            }

            return result;
        }
    }

     //   f [ r L ] l L

    private string Replace(string token)
    {
        foreach(Production production in productionRules)
        {
            if (production.lhs.Equals(token))
            {
                return production.rhs;
            }
        }
        return token;
    }

    private void SpawnLeaf(Vector3 position, Quaternion rotation)
    {
        GameObject leaf = Instantiate(leafBlob, position, rotation, transform);
        float scaleX = UnityEngine.Random.Range(0.5f, 1.5f);
        float scaleY = UnityEngine.Random.Range(0.5f, 1.5f);
        float scaleZ = UnityEngine.Random.Range(0.5f, 1.5f);

        leaf.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    private void Interpret(string tree)
    {
        Vector3 position = transform.position;
        Quaternion rotation = Quaternion.identity;

        Stack<(Vector3, Quaternion)> stack = new Stack<(Vector3, Quaternion)>();

        foreach (string token in tree.Split(" "))
        {
            switch(token)
            {
                case "f":
                    GameObject g = Instantiate(treeBranch, position, rotation, transform);
                    position += g.transform.up * 1;
                    if (UnityEngine.Random.value < leafChance)
                    {
                        SpawnLeaf(position, rotation);
                    }
                    break;

                case "l":
                    float leftOrRight = UnityEngine.Random.Range(-40.0f, 40.0f);
                    float plusSide = UnityEngine.Random.Range(5.0f, 40.0f);
                    rotation *= Quaternion.AngleAxis(plusSide, Vector3.forward);
                    rotation *= Quaternion.AngleAxis(leftOrRight, Vector3.right);
                    if (UnityEngine.Random.value < leafChance)
                    {
                        SpawnLeaf(position, rotation);
                    }
                    break;

                case "r":
                    float rightOrLeft = UnityEngine.Random.Range(-40.0f, 40.0f);
                    float minusSide = UnityEngine.Random.Range(-40.0f, -5.0f);
                    rotation *= Quaternion.AngleAxis(minusSide, Vector3.forward);
                    rotation *= Quaternion.AngleAxis(rightOrLeft, Vector3.left);
                    if (UnityEngine.Random.value < leafChance)
                    {
                        SpawnLeaf(position, rotation);
                    }
                    break;

                case "[":
                    stack.Push((position, rotation)); 
                    break;

                case "]":
                    (position, rotation) = stack.Pop();
                    break;
            }
        }
    }
}
