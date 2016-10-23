using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Class for rendering a volumetric dataset as particles.
/// The dataset contains at least XYZ coordinates. 
/// Using a custom colour gradient, any value within the dataset can be translated into colour and opacity
/// </summary>
///

[RequireComponent(typeof(ParticleSystem))]

public class ParticleDatasetRenderer : MonoBehaviour
{
	[Tooltip("Text/CSV file with the data")]
	public TextAsset DataSource;

	[Tooltip("Size of an individual particle")]
	public float particleSize = 0.1f;

	[Tooltip("Color and opacity map of an individual particle based on some chosen variable")]
	public Gradient particleColorMap;

	/// <summary>
	/// Structure for holding the raw data from the text/CSV file
	/// </summary>
	///
	private struct DatasetEntry
	{
		public Vector3 position;
		public Vector3 velocity;
		public float   velocityMagnitude;
		public float   positionMagnitude;
	}


	void Start ()
	{
		// get the particle system component
		particleSystem  = GetComponent<ParticleSystem>();

		// initialise variables
		dataCentrePoint = new Vector3();
		dataset         = new List<DatasetEntry>();


		// read data
		ReadDataset();

		// generate particles
		int datasetSize = dataset.Count;
		particles = new ParticleSystem.Particle[datasetSize];
		Debug.Log("Generating " + datasetSize + " particles");
		for ( int i = 0; i < datasetSize; i++ )
		{
			particles[i] = new ParticleSystem.Particle();
			particles[i].lifetime      = float.MaxValue; // particle lives forever
			particles[i].startLifetime = float.MaxValue;
			particles[i].position      = dataset[i].position - dataCentrePoint; // offset by centre point

		}

		UpdateData();
	}
	

	private void ReadDataset()
	{
		// clear previous data
		dataset.Clear();
		maxVelocity     = 0;
		maxPosition     = 0;
		dataCentrePoint = Vector3.zero;

		// read the data from the text/CSV file
		string[] lines = DataSource.text.Split('\n'); // split lines 

		// never create more than the max number of particles stated in the Particle System inspector
		int datasetSize = Mathf.Min(lines.Length - 1, particleSystem.maxParticles); //(-1: ignore header)
		Debug.Log("Dataset size: " + (lines.Length - 1) + " rows");

		// taking randomly selected particles
		int[] datasetindices = new int[lines.Length - 1]; //array for indices of lines
		float[] order = new float[lines.Length - 1]; //parallel array for sorting indices

		for (int ctr = 0; ctr < order.Length; ctr++)
		{
			datasetindices[ctr] = ctr + 1; //populating index array with indices (+1: ignore header)
			order[ctr] = Random.value; //assigning random value between 0.0 and 1.0 (inclusive) to each element of array
		}

		System.Array.Sort (order, datasetindices); //sort indices into randomised order

		// read data
		for (int i = 0; i < datasetSize; i++)
		{
			// parse data from text/CSV file
			string line = lines[datasetindices[i]].Trim(); 

			//if (line.Length == 0) break; // empty line marks end of dataset (don't use this if randomising order)


			string[]     txtData = line.Split(' '); // get line (+1: ignore header), split around spaces to get each quantity (use ',' for CSV file)
			DatasetEntry entry   = new DatasetEntry();
			entry.position = new Vector3(float.Parse(txtData[0]), // position XYZ in 1st to 3rd column
			                             float.Parse(txtData[1]),
			                             float.Parse(txtData[2]));
			entry.velocity = new Vector3(float.Parse(txtData[3]), // velocity XYZ in 4th to 6th column
			                             float.Parse(txtData[4]),
			                             float.Parse(txtData[5]));
			entry.velocityMagnitude = entry.velocity.magnitude;    // cache velocity magnitude for faster updates
			entry.positionMagnitude = entry.position.magnitude;    // cache position magnitude for faster updates


			dataset.Add(entry);

			dataCentrePoint += entry.position;
			maxVelocity = Mathf.Max(maxVelocity, entry.velocityMagnitude);
			maxPosition = Mathf.Max (maxPosition, entry.positionMagnitude);

		}
		datasetSize = dataset.Count;
		dataCentrePoint /= datasetSize;
		Debug.Log("Dataset centre point: " + dataCentrePoint);
		Debug.Log("Maximum velocity: " + maxVelocity);
	}


	/// <summary>
	/// Recalculate sizes and colours of the particles.
	/// </summary>
	///
	public void UpdateData()
	{
		if (particles == null) return;

		for (int i = 0; i < particles.Length; i++)
		{
			// set colour and size
			//particles[i].startColor = particleColorMap.Evaluate(dataset[i].velocityMagnitude / maxVelocity);  //colour as function of speed
			particles [i].startColor = particleColorMap.Evaluate((dataset[i].position - dataCentrePoint).magnitude / (maxPosition - dataCentrePoint.magnitude));  //colour as function of distance from dataset centre
			particles[i].startSize  = particleSize;    //set in the Particle System inspector
		}
		particleSystem.SetParticles(particles, particles.Length);
	}


	void Update()
	{
		// nothing to do here

	}


	/// <summary>
	/// Make sure every update in the inspector immediately has an effect on the data.
	/// </summary>
	///
	void OnValidate()
	{
		UpdateData();
	}


	new private ParticleSystem        particleSystem;
	private List<DatasetEntry>        dataset;

	private Vector3                   dataCentrePoint;
	private float                     maxVelocity;
	private float                     maxPosition;


	private ParticleSystem.Particle[] particles;
}
