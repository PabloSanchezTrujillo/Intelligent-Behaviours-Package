using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class MakePizza : MonoBehaviour {

    #region variables

    [Header("Positions")]
    [SerializeField] private Transform pizzaPosition;

    [SerializeField] private Transform tomatoPosition;
    [SerializeField] private Transform firstTopping;
    [SerializeField] private Transform secondTopping;
    [SerializeField] private Transform thirdTopping;
    [SerializeField] private Transform ovenPosition;
    [SerializeField] private Transform tablePosition;

    [Header("Game objects")]
    [SerializeField] private GameObject pizzaMass;

    [SerializeField] private GameObject pizzaTomato;
    [SerializeField] private GameObject[] HamAndCheese = new GameObject[3];
    [SerializeField] private GameObject[] Vegetarian = new GameObject[3];
    [SerializeField] private GameObject[] Hawaiian = new GameObject[2];

    [Header("Texts")]
    [SerializeField] private Text recipeName;

    [SerializeField] private Text[] toppingTexts = new Text[3];

    private BehaviourTreeEngine behaviourTree;
    private StateMachineEngine stateMachine;
    private Perception massPutted;
    private Perception tomatoPutted;
    private Perception nextTopping;
    private Perception allToppings;
    private LeafNode subFSM;
    private GameObject[] actualRecipe;
    private int actualIngredient;
    private GameObject pizza;
    private GameObject recipe;
    private Animator recipeAnimator;
    private GameObject handler;
    private NavMeshAgent meshAgent;

    #endregion variables

    // Start is called before the first frame update
    private void Start()
    {
        behaviourTree = new BehaviourTreeEngine(BehaviourEngine.IsNotASubmachine);
        stateMachine = new StateMachineEngine(BehaviourEngine.IsASubmachine);
        recipe = GameObject.FindGameObjectWithTag("Recipe");
        recipeAnimator = recipe.GetComponent<Animator>();
        handler = transform.GetChild(5).gameObject;
        meshAgent = GetComponent<NavMeshAgent>();

        CreateStateMachine();
        CreateBehaviourTree();
    }

    private void CreateBehaviourTree()
    {
        LeafNode lookRecipe = behaviourTree.CreateLeafNode("Look recipe", CreateRecipe, IsRecipeCreated);
        LeafNode bake = behaviourTree.CreateLeafNode("Bake pizza", BakePizza, IsPizzaBaked);
        SequenceNode makePizza = behaviourTree.CreateSequenceNode("Make a pizza", false);
        makePizza.AddChild(lookRecipe);
        makePizza.AddChild(subFSM);
        makePizza.AddChild(bake);
        LoopDecoratorNode loopNode = behaviourTree.CreateLoopNode("Loop root", makePizza);

        behaviourTree.SetRootNode(loopNode);
    }

    private void CreateStateMachine()
    {
        // Perceptions
        massPutted = stateMachine.CreatePerception<TimerPerception>(1);
        tomatoPutted = stateMachine.CreatePerception<TimerPerception>(1);
        nextTopping = stateMachine.CreatePerception<TimerPerception>(1);
        allToppings = stateMachine.CreatePerception<PushPerception>();

        // States
        State putMass = stateMachine.CreateEntryState("Put mass", PutMass);
        State putTomato = stateMachine.CreateState("Put tomato", PutTomato);
        State putTopping = stateMachine.CreateState("Put topping", PutTopping);

        // Transitions
        stateMachine.CreateTransition("Mass putted", putMass, massPutted, putTomato);
        stateMachine.CreateTransition("Tomato putted", putTomato, tomatoPutted, putTopping);
        stateMachine.CreateTransition("Next topping", putTopping, nextTopping, putTopping);

        // Super-node of the Behaviour Tree and exit transition
        subFSM = behaviourTree.CreateSubBehaviour("Sub-FSM", stateMachine, putMass);
        stateMachine.CreateExitTransition("Back to BT", putTopping, allToppings, ReturnValues.Succeed);
    }

    // Update is called once per frame
    private void Update()
    {
        behaviourTree.Update();
        stateMachine.Update();
    }

    private void CreateRecipe()
    {
        meshAgent.SetDestination(tablePosition.position);
        recipeAnimator.SetTrigger("Reset");
        switch(Random.Range(1, 4)) {
            case 1:
                actualRecipe = HamAndCheese;
                recipeName.text = "Pepperoni";
                break;

            case 2:
                actualRecipe = Vegetarian;
                recipeName.text = "Vegetarian";
                break;

            case 3:
                actualRecipe = Hawaiian;
                recipeName.text = "Hawaiian";
                break;
        }
        for(int i = 0; i < actualRecipe.Length; i++) {
            toppingTexts[i].text = actualRecipe[i].name;
        }

        actualIngredient = 0;
    }

    private ReturnValues IsRecipeCreated()
    {
        if(Vector3.Distance(transform.position, tablePosition.position) < 0.1f) {
            transform.LookAt(pizzaPosition);
            recipeAnimator.ResetTrigger("Reset");
            return ReturnValues.Succeed;
        }
        else {
            return ReturnValues.Running;
        }
    }

    private void BakePizza()
    {
        pizza.transform.parent = handler.transform;
        pizza.transform.localPosition = Vector3.zero;
        meshAgent.SetDestination(ovenPosition.position);
    }

    private ReturnValues IsPizzaBaked()
    {
        if(Vector3.Distance(transform.position, ovenPosition.position) < 0.1f) {
            Destroy(pizza);
            foreach(Text text in toppingTexts) {
                text.text = "";
            }
            return ReturnValues.Succeed;
        }
        else {
            return ReturnValues.Running;
        }
    }

    private void PutMass()
    {
        pizza = Instantiate(pizzaMass, pizzaPosition.position, pizzaPosition.rotation);
    }

    private void PutTomato()
    {
        Instantiate(pizzaTomato, tomatoPosition.position, tomatoPosition.rotation, pizza.transform);
    }

    private void PutTopping()
    {
        switch(actualIngredient) {
            case 0:
                Instantiate(actualRecipe[actualIngredient], firstTopping.position, firstTopping.rotation, pizza.transform);
                break;

            case 1:
                Instantiate(actualRecipe[actualIngredient], secondTopping.position, thirdTopping.rotation, pizza.transform);
                break;

            case 2:
                Instantiate(actualRecipe[actualIngredient], thirdTopping.position, thirdTopping.rotation, pizza.transform);
                break;
        }
        actualIngredient++;

        if(actualIngredient == actualRecipe.Length) {
            allToppings.Fire();
        }
    }
}