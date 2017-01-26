using UnityEngine;

public class StuffSpawner : MonoBehaviour {

	public float velocity;
	public Stuff[] stuffPrefabs;
	public FloatRange timeBetweenSpawns, scale, randomVelocity, angularVelocity;
	public Material stuffMaterial;

	float timeSinceLastSpawn;
	float currentSpawnDelay;

	void FixedUpdate () {
		timeSinceLastSpawn += Time.deltaTime;
		if (timeSinceLastSpawn >= currentSpawnDelay) {
			timeSinceLastSpawn -= currentSpawnDelay;
			currentSpawnDelay = timeBetweenSpawns.RandomInRange;
			SpawnStuff();
		}
	}

	void SpawnStuff () {
		Stuff prefab = stuffPrefabs[Random.Range(0, stuffPrefabs.Length)];

		//Stuff spawn = Instantiate<Stuff>(prefab);
		Stuff spawn = prefab.GetPooledInstance<Stuff>();

		spawn.transform.localPosition = transform.position;
		spawn.transform.localScale = Vector3.one * scale.RandomInRange;
		spawn.transform.localRotation = Random.rotation;
		spawn.Body.velocity = transform.up * velocity + Random.onUnitSphere * randomVelocity.RandomInRange;
		spawn.Body.angularVelocity = Random.onUnitSphere * angularVelocity.RandomInRange;
		//spawn.GetComponent<MeshRenderer>().material = stuffMaterial;
		spawn.SetMaterial(stuffMaterial);
	}
}