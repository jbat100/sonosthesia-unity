using UnityEngine;
using System.Collections;

public class Fractal : MonoBehaviour {

	public Material material;
	public int maxDepth;
	public float childScale;
	public Mesh[] meshes;
	public float spawnProbability;
	public float maxRotationSpeed;
	public float maxTwist;

	private float rotationSpeed;
	private int depth;

	private static Vector3[] childDirections = {
		Vector3.up,
		Vector3.right,
		Vector3.left,
		Vector3.forward,
		Vector3.back
	};

	private static Quaternion[] childOrientations = {
		Quaternion.identity,
		Quaternion.Euler(0f, 0f, -90f),
		Quaternion.Euler(0f, 0f, 90f),
		Quaternion.Euler(90f, 0f, 0f),
		Quaternion.Euler(-90f, 0f, 0f)
	};

	private Material[,] materials;

	private void InitializeMaterials () {
		materials = new Material[maxDepth + 1, 2];
		for (int i = 0; i <= maxDepth; i++) {
			float t = i / (maxDepth - 1f);
			t *= t;
			materials[i, 0] = new Material(material);
			materials[i, 0].color = Color.Lerp(Color.white, Color.yellow, t);
			materials[i, 1] = new Material(material);
			materials[i, 1].color = Color.Lerp(Color.white, Color.cyan, t);
		}
		//materials[maxDepth, 0].color = Color.magenta;
		//materials[maxDepth, 1].color = Color.red;
	}

	// Use this for initialization
	void Start () {

		if (materials == null) {
			InitializeMaterials();
		}

		rotationSpeed = Random.Range(-maxRotationSpeed, maxRotationSpeed);
		transform.Rotate(Random.Range(-maxTwist, maxTwist), 0f, 0f);

		gameObject.AddComponent<MeshFilter>().mesh = meshes[Random.Range(0, meshes.Length)];
		gameObject.AddComponent<MeshRenderer>().material = materials[depth, Random.Range(0, 2)];

		if (depth < maxDepth) {
			StartCoroutine(CreateChildren());
		}
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
	}

	private void Initialize (Fractal parent, Vector3 direction, Quaternion orientation) {

		meshes = parent.meshes;
		materials = parent.materials;
		maxDepth = parent.maxDepth;
		spawnProbability = parent.spawnProbability;
		childScale = parent.childScale;
		maxRotationSpeed = parent.maxRotationSpeed;
		maxTwist = parent.maxTwist;
		transform.parent = parent.transform;

		depth = parent.depth + 1;
		transform.localScale = Vector3.one * childScale;
		transform.localPosition = direction * (0.5f + 0.5f * childScale);
		transform.localRotation = orientation;
	}

	private void InitializeIndex (Fractal parent, int i) {
		Initialize(parent, childDirections[i], childOrientations[i]);
	}

	private IEnumerator CreateChildren () {
		for (int i = 0; i < childDirections.Length; i++) {
			if (Random.value < spawnProbability) {
				yield return new WaitForSeconds(Random.Range(0.1f, 0.2f));
				new GameObject("Fractal Child").AddComponent<Fractal>().InitializeIndex(this, i);
			}
		}
	}
}
