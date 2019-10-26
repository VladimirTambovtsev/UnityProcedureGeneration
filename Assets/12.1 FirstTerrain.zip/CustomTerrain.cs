using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour {

	public Vector2 randomHeightRange = new Vector2 (0.0f, 0.1f);
	public Texture2D heightMapImage;
	public Vector3 heightMapScale = new Vector3(1, 1, 1);

	public bool resetTerrain = true;
    
	// Perlin Noise -------------------------------------------------------
	public float perlinXScale = 0.01f;
	public float perlinYScale = 0.01f;
	public int perlinOffsetX = 0;
	public int perlinOffsetY = 0;
	public int perlinOctaves = 3;
	public float perlinPersistance = 8;
	public float perlinHeightScale = 0.09f;

	// Mulitple Perlin ---------------------------------------------------
	[System.Serializable]
	public class PerlinParameters {
		public float mPerlinXScale = 0.01f;
		public float mPerlinYScale = 0.01f;
		public int mPerlinOctaves = 3;
		public float mPerlinPersistance = 8;
		public float mPerlinHeightScale = 0.09f;
		public int mPerlinOffsetX = 0;
		public int mPerlinOffsetY = 0;
		public bool remove = false;
	}

	public List<PerlinParameters> perlinParameters = new List<PerlinParameters> () {
		new PerlinParameters()
	};

    // SplatMaps
    [System.Serializable]
    public class SplatHeights {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0.0f;
        public float maxSlope = 1.5f;
        public Vector2 tillOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
        public float splatOffset = 0.1f;
        public float splatNoiseXScale = 0.01f;
        public float splatNoiseYScale = 0.01f;
        public float splatNoiseScaler = 0.1f;
        public bool remove = false;
    }


    public List<SplatHeights> splatHeights = new List<SplatHeights>() {
        new SplatHeights()
    };

    // Vegetation ----------------------------------------------------
    [System.Serializable]
    public class Vegetation {
        public GameObject mesh;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
        public Color colour1 = Color.white;
        public Color colour2 = Color.white;
        public Color lightColour = Color.white;
        public float minRotation = 0.0f;
        public float maxRotation = 360.0f;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Vegetation> vegetation = new List<Vegetation>() {
        new Vegetation()
    };

    public int maxTrees = 5000;
    public int treeSpacing = 5;

    // Details --------------------------------------------------------
    [System.Serializable]
    public class Detail {
        public GameObject prototype = null;
        public Texture2D prototypeTexture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 1;
        public Color dryColour = Color.white;
        public Color healthyColour = Color.white;
        public Vector2 heightRange = new Vector2(1, 1);
        public Vector2 widthRange = new Vector2(1, 1);
        public float noiseSpread = 0.5f;
        public float overlap = 0.01f;
        public float feather = 0.05f;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Detail> details = new List<Detail>() {
        new Detail()
    };

    public int maxDetails = 5000;
    public int detailSpacing = 5;

    // Voronoi --------------------------------------------------------
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public int voronoiPeaks = 5;
    public enum VoronoiType {  Linear, Power, Combined, Yurt }
    public VoronoiType voronoiType = VoronoiType.Linear;

    // Midpoint Displacement -----------------------------------------
    public float MPDheightMin = -2.0f;
    public float MPDheightMax = 2.0f;
    public float MPDheightDampenerPower = 2.0f;
    public float MPDroughness = 2.0f;

    // Smoothing -----------------------------------------------------
    public int smoothAmount = 2;

	public Terrain terrain;
	public TerrainData terrainData;

    public void AddDetails() {
        DetailPrototype[] newDetailPrototypes;
        newDetailPrototypes = new DetailPrototype[details.Count];
        int dIndex = 0;
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth,
                                            terrainData.heightmapHeight);

        foreach (Detail d in details) {
            newDetailPrototypes[dIndex] = new DetailPrototype();
            newDetailPrototypes[dIndex].prototype = d.prototype;
            newDetailPrototypes[dIndex].prototypeTexture = d.prototypeTexture;
            newDetailPrototypes[dIndex].healthyColor = d.healthyColour;
            newDetailPrototypes[dIndex].dryColor = d.dryColour;
            newDetailPrototypes[dIndex].minHeight = d.heightRange.x;
            newDetailPrototypes[dIndex].maxHeight = d.heightRange.y;
            newDetailPrototypes[dIndex].minWidth = d.widthRange.x;
            newDetailPrototypes[dIndex].maxWidth = d.widthRange.y;
            newDetailPrototypes[dIndex].noiseSpread = d.noiseSpread;

            if (newDetailPrototypes[dIndex].prototype) {
                newDetailPrototypes[dIndex].usePrototypeMesh = true;
                newDetailPrototypes[dIndex].renderMode = DetailRenderMode.VertexLit;
            } else {
                newDetailPrototypes[dIndex].usePrototypeMesh = false;
                newDetailPrototypes[dIndex].renderMode = DetailRenderMode.GrassBillboard;
            }
            dIndex++;
        }
        terrainData.detailPrototypes = newDetailPrototypes;

        for(int i = 0; i < terrainData.detailPrototypes.Length; ++i) {
            int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];
            for(int y = 0; y < terrainData.detailHeight; y += detailSpacing) {
                for(int x = 0; x < terrainData.detailWidth; x += detailSpacing) {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > details[i].density) continue;
        
                    int xHM = (int)(x / (float)terrainData.detailWidth * terrainData.heightmapWidth);
                    int yHM = (int)(y / (float)terrainData.detailHeight * terrainData.heightmapHeight);

                    float thisNoise = Utils.Map(Mathf.PerlinNoise(x * details[i].feather,
                                                y * details[i].feather), 0, 1, 0.5f, 1);
                    float thisHeightStart = details[i].minHeight * thisNoise -
                                            details[i].overlap * thisNoise;
                    float nextHeightStart = details[i].maxHeight * thisNoise +
                                            details[i].overlap* thisNoise;

                    float thisHeight = heightMap[yHM, xHM];
                    float steepness = terrainData.GetSteepness( xHM / (float)terrainData.size.x,
                                                                yHM / (float)terrainData.size.z);
                    if((thisHeight >= thisHeightStart && thisHeight <= nextHeightStart) &&
                        (steepness >= details[i].minSlope && steepness <= details[i].maxSlope)) {
                        detailMap[y, x] = 1;
                    }
                }
            }
            terrainData.SetDetailLayer(0, 0, i, detailMap);
        }
    }

    public void AddNewDetails() {
        details.Add(new Detail());
    }

    public void RemoveDetails() {
        List<Detail> keptDetails = new List<Detail>();
        for (int i = 0; i < details.Count; ++i) {
            if (!details[i].remove) {
                keptDetails.Add(details[i]);
            }
        }
        if (keptDetails.Count == 0) {    // Don't want to keep any
            keptDetails.Add(details[0]);  // Add at least one;
        }
        details = keptDetails;
    }

    public void PlantVegetaion() {
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int tIndex = 0;
        foreach (Vegetation t in vegetation) {
            newTreePrototypes[tIndex] = new TreePrototype();
            newTreePrototypes[tIndex].prefab = t.mesh;
            tIndex++;
        }
        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();

        for (int z = 0; z < terrainData.size.z; z += treeSpacing) {
            for(int x = 0; x < terrainData.size.z; x += treeSpacing) {
                for(int tp = 0; tp < terrainData.treePrototypes.Length; ++tp) {

                    if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density) break;
                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;

                    float steepness = terrainData.GetSteepness( x / (float)terrainData.size.x,
                                                                z / (float)terrainData.size.z);

                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) && 
                            (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope)) {
                        TreeInstance instance = new TreeInstance();
                        instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.x,
                                                        terrainData.GetHeight(x, z) / terrainData.size.y,
                                                        (z + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);
                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                            instance.position.y * terrainData.size.y,
                            instance.position.z * terrainData.size.z)
                            + this.transform.position;

                        RaycastHit hit;
                        int layerMask = 1 << terrainLayer;
                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) ||
                            Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask)) {
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            instance.position = new Vector3(instance.position.x,
                                treeHeight,
                                instance.position.z);
                        }
                        instance.rotation = 
                            UnityEngine.Random.Range(vegetation[tp].minRotation, vegetation[tp].maxRotation);
                        instance.prototypeIndex = tp;
                        instance.color = Color.Lerp(vegetation[tp].colour1,
                                                    vegetation[tp].colour2,
                                                    UnityEngine.Random.Range(0.0f, 1.0f));
                        instance.lightmapColor = vegetation[tp].lightColour;
                        float s = 
                            UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                        instance.heightScale = s;
                        instance.widthScale = s;

                        allVegetation.Add(instance);
                        if (allVegetation.Count >= maxTrees) goto TREESDONE;

                    }
                }
            }
        }
        TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();
    }

    public void AddNewVegetaion() {
        vegetation.Add(new Vegetation());
    }

    public void RemoveVegetaion() {
        List<Vegetation> keptVegetation = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; ++i) {
            if (!vegetation[i].remove) {
                keptVegetation.Add(vegetation[i]);
            }
        }
        if (keptVegetation.Count == 0) {    // Don't want to keep any
            keptVegetation.Add(vegetation[0]);  // Add at least one;
        }
        vegetation = keptVegetation;
    }

    public void AddNewSplatHeight() {
        splatHeights.Add(new SplatHeights());
    }

    public void RemoveSplatHeight() {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();

        for (int i = 0; i < splatHeights.Count; ++i) {
            if (!splatHeights[i].remove) {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }

        if (keptSplatHeights.Count == 0) {  // Don't want to keep any
            keptSplatHeights.Add(splatHeights[0]);  // Add at least one
        }

        splatHeights = keptSplatHeights;
    }

    float GetSteepness(float[,] heightMap, int x, int y, int width, int height) {
        float h = heightMap[x, y];
        int nX = x + 1;
        int nY = y + 1;

        // If on the upper edge of the map find gradient by going backward
        if (nX > width - 1) {
            nX = x - 1;
        }

        if (nY > height - 1) {
            nY = y - 1;
        }

        float dX = heightMap[nX, y] - h;
        float dY = heightMap[x, nY] - h;

        Vector2 gradient = new Vector2(dX, dY);

        float steep = gradient.magnitude;
        return steep;
    }

    public void SplatMaps() {
        SplatPrototype[] newSplatPrototypes;
        newSplatPrototypes = new SplatPrototype[splatHeights.Count];
        int spIndex = 0;

        foreach (SplatHeights sh in splatHeights) {
            newSplatPrototypes[spIndex] = new SplatPrototype();
            newSplatPrototypes[spIndex].texture = sh.texture;
            newSplatPrototypes[spIndex].texture.Apply(true);
            newSplatPrototypes[spIndex].tileSize = sh.tileSize;
            newSplatPrototypes[spIndex].texture.Apply(true);
            spIndex++;
        }
        terrainData.splatPrototypes = newSplatPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth,
                                            terrainData.heightmapHeight);
        float[,,] splatmapData = new float[terrainData.alphamapWidth,
                                            terrainData.alphamapHeight,
                                            terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; ++y) {
            for (int x = 0; x < terrainData.alphamapWidth; ++x) {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; ++i) {
                    float noise = Mathf.PerlinNoise(x   * splatHeights[i].splatNoiseXScale, y 
                                                        * splatHeights[i].splatNoiseYScale) 
                                                        * splatHeights[i].splatNoiseScaler;
                    float offset = splatHeights[i].splatOffset + noise;
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    //float steepness = GetSteepness( heightMap, x, y, 
                    //                                terrainData.heightmapWidth,
                    //                                terrainData.heightmapHeight);
                    float steepness = terrainData.GetSteepness( y / (float)terrainData.alphamapHeight,
                                                                x / (float)terrainData.alphamapWidth);

                    if ((heightMap[x, y] >= thisHeightStart && 
                        heightMap[x, y] <= thisHeightStop) && 
                        (steepness >= splatHeights[i].minSlope && 
                        steepness <= splatHeights[i].maxSlope)) {
                        splat[i] = 1;
                    }
                }
                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; ++j) {
                    splatmapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    void NormalizeVector(float[] v) {
        float total = 0;
        for (int i = 0; i < v.Length; ++i) {
            total += v[i];
        }

        for (int i = 0; i < v.Length; ++i) {
            v[i] /= total;
        }
    }

	float[,] GetHeightMap() {
		if (!resetTerrain) {
			return terrainData.GetHeights (0, 0, terrainData.heightmapWidth, 
                terrainData.heightmapHeight);
		} else {
			return new float[terrainData.heightmapWidth, 
                terrainData.heightmapHeight];
		}
	}

    public void Voronoi() {
        float[,] heightMap = GetHeightMap();

        for (int p = 0; p < voronoiPeaks; ++p) {

            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapWidth),
                UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                UnityEngine.Random.Range(0, terrainData.heightmapHeight));
            if (heightMap[(int)peak.x, (int)peak.z] < peak.y) {
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            }
            else {
                continue;
            }

            Vector2 peakLocation = new Vector2(peak.x, peak.z);

            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapWidth,
                terrainData.heightmapHeight));

            for (int y = 0; y < terrainData.heightmapHeight; ++y) {
                for (int x = 0; x < terrainData.heightmapWidth; ++x) {
                    if (!(x == peak.x && y == peak.z)) {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;
                        if (voronoiType == VoronoiType.Combined) {
                            h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff);
                        }   // Combined 
                        else if (voronoiType == VoronoiType.Power) {
                            h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;    // Power
                        }
                        else if (voronoiType == VoronoiType.Yurt) { // Yurt
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) - 
                                Mathf.Sin(distanceToPeak * 2.0f * Mathf.PI) / voronoiDropOff;
                        }
                        else {
                            h = peak.y - distanceToPeak * voronoiFallOff;   // Linear
                        }
                        if (heightMap[x, y] < h) {
                            heightMap[x, y] = h;
                        }
                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);

    }

	public void Perlin(){
		float[,] heightMap = GetHeightMap ();

		for (int y = 0; y < terrainData.heightmapHeight; ++y) {
			for (int x = 0; x < terrainData.heightmapWidth; ++x) {
				heightMap [x, y] += Utils.fBM ((x + perlinOffsetX) * perlinXScale,
					(y + perlinOffsetY) * perlinYScale,
					perlinOctaves,
					perlinPersistance) * perlinHeightScale;
			}
		}
		terrainData.SetHeights (0, 0, heightMap);
	}
    
	public void MultiplePerlinTerrain() {
		float[,] heightMap = GetHeightMap ();

		for (int y = 0; y < terrainData.heightmapHeight; ++y) {
			for (int x = 0; x < terrainData.heightmapWidth; ++x) {
				foreach (PerlinParameters p in perlinParameters) {
					heightMap [x, y] += Utils.fBM ((x + p.mPerlinOffsetX) * p.mPerlinXScale,
						(y + p.mPerlinOffsetY) * p.mPerlinYScale, 
						p.mPerlinOctaves, 
						p.mPerlinPersistance) * p.mPerlinHeightScale;
				}
			}
		}
		terrainData.SetHeights (0, 0, heightMap);
	}

	public void AddNewPerlin() {
		perlinParameters.Add (new PerlinParameters ());
	}

	public void RemovePerlin() {
		List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters> ();
		for (int i = 0; i < perlinParameters.Count; ++i) {
			if (!perlinParameters [i].remove) {
				keptPerlinParameters.Add (perlinParameters [i]);
			}
		}

		if (keptPerlinParameters.Count == 0) {	// Don't want to keep any
			keptPerlinParameters.Add(perlinParameters[0]);	// Add at least 1
		}
		perlinParameters = keptPerlinParameters;
	}

	public void RandomTerrain(){
		float[,] heightMap = GetHeightMap ();

		for (int x = 0; x < terrainData.heightmapWidth; ++x) {
			for (int z = 0; z < terrainData.heightmapHeight; ++z) {
				heightMap [x, z] += UnityEngine.Random.Range (randomHeightRange.x, randomHeightRange.y);
			}
		}
		terrainData.SetHeights (0, 0, heightMap);
	}

	public void LoadTexture() {
		float[,] heightMap = GetHeightMap ();
		for (int x = 0; x < terrainData.heightmapWidth; ++x) {
			for (int z = 0; z < terrainData.heightmapHeight; ++z) {
				heightMap [x, z] += heightMapImage.GetPixel ((int)(x * heightMapScale.x),
					(int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
			}
		}
		terrainData.SetHeights (0, 0, heightMap);
	}

    public void MidpointDisplacement() {
        float[,] heightMap = GetHeightMap();
        int width = terrainData.heightmapWidth - 1;
        int squareSize = width;
        float heightMin = MPDheightMin;
        float heightMax = MPDheightMax;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        //heightMap[0, 0] = UnityEngine.Random.Range(0.0f, 0.2f);
        //heightMap[0, terrainData.heightmapHeight - 2] = UnityEngine.Random.Range(0.0f, 0.2f);
        //heightMap[terrainData.heightmapWidth - 2, 0] = UnityEngine.Random.Range(0.0f, 0.2f);
        //heightMap[terrainData.heightmapWidth - 2, terrainData.heightmapHeight - 2] =
        //    UnityEngine.Random.Range(0.0f, 0.2f);

        while (squareSize > 0) {
            for (int x = 0; x < width; x += squareSize) {
                for (int y = 0; y < width; y += squareSize) {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                        heightMap[cornerX, y] +
                        heightMap[x, cornerY] +
                        heightMap[cornerX, cornerY]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            for(int x = 0; x < width; x += squareSize) {
                for (int y = 0; y < width; y += squareSize) {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1) {
                        continue;
                    }

                    // Calculate the square value for the bottom side
                    heightMap[midX, y] = (float)((heightMap[midX, midY] +
                        heightMap[x, y] +
                        heightMap[midX, pmidYD] +
                        heightMap[cornerX, y]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));

                    // Calculate the square value for the top side
                    heightMap[midX, cornerY] = (float)((heightMap[midX, pmidYU] + 
                        heightMap[x, cornerY] +
                        heightMap[midX, midY] + 
                        heightMap[cornerX, cornerY]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));

                    // Calculate the square value for the left side
                    heightMap[x, midY] = (float)((heightMap[x, cornerY] +
                        heightMap[pmidXL, midY] +
                        heightMap[x, y] +
                        heightMap[midX, midY]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));

                    // Calculate the square value for the right side
                    heightMap[midX, y] = (float)((heightMap[cornerX, cornerY] +
                        heightMap[midX, midY] +
                        heightMap[cornerX, y] +
                        heightMap[pmidXR, midY]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));

                    heightMap[midX, y] = (float)((heightMap[midX, midY] +
                        heightMap[x, y] +
                        heightMap[midX, pmidYD] +
                        heightMap[cornerX, y]) / 4.0f +
                        UnityEngine.Random.Range(heightMin, heightMax));
                }
            }
            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height) {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; ++y) {
            for (int x = -1; x < 2; ++x) {
                if (!(x == 0 && y == 0)) {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1),
                        Mathf.Clamp(pos.y + y, 0, height - 1));
                    if (!neighbours.Contains(nPos)) {
                        neighbours.Add(nPos);
                    }
                }
            }
        }
        return neighbours;
    }

    public void SmoothTerrain() {

        float[,] heightMap = GetHeightMap();
        float smoothProgress = 0;
        EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);
        for (int noOfSmooths = 0; noOfSmooths < smoothAmount; ++noOfSmooths) {
            for (int y = 0; y < terrainData.heightmapHeight; ++y) {
                for (int x = 0; x < terrainData.heightmapWidth; ++x) {
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y),
                        terrainData.heightmapWidth,
                        terrainData.heightmapHeight);

                    foreach (Vector2 n in neighbours) {
                        avgHeight += heightMap[(int)n.x, (int)n.y];
                    }

                    heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
                }
            }
            smoothProgress++;
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress / smoothAmount);
        }
        terrainData.SetHeights(0, 0, heightMap);
        EditorUtility.ClearProgressBar();
    }

	public void ResetTerrain() {
		float[,] heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
		for (int x = 0; x < terrainData.heightmapWidth; ++x) {
			for (int z = 0; z < terrainData.heightmapHeight; ++z) {
				heightMap [x, z] = 0;
			}
		}
		terrainData.SetHeights (0, 0, heightMap);
	}

	void OnEnable() {
		Debug.Log ("Initialising Terrain Data");
		terrain = this.GetComponent<Terrain> ();
		terrainData = Terrain.activeTerrain.terrainData;
	}

    public enum TagType { Tag, Layer };
    [SerializeField]
    int terrainLayer = -1;

	void Awake(){
		SerializedObject tagManager = new SerializedObject(
			AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		SerializedProperty tagsProp = tagManager.FindProperty ("tags");

		AddTag (tagsProp, "Terrain", TagType.Tag);
		AddTag (tagsProp, "Cloud", TagType.Tag);
		AddTag (tagsProp, "Shore", TagType.Tag);

		// Apply tag changes to tag database
		tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

		// Take this object
		this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;
	}

	int AddTag(SerializedProperty tagsProp, string newTag, TagType tType){
		bool found = false;

		// Ensure the tag doesn't already exist
		for(int i = 0; i < tagsProp.arraySize; ++i){
			SerializedProperty t = tagsProp.GetArrayElementAtIndex (i);
			if (t.stringValue.Equals (newTag)) {
				found = true;
				return i;
			}
		}

        // Add your new tag
        if (!found && tType == TagType.Tag) {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }

        // Add new layer
        else if (!found && tType == TagType.Layer) {
            for(int j = 8; j < tagsProp.arraySize; ++j) {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                // Add layer in next slot
                if (newLayer.stringValue == "") {
                    Debug.Log("Adding New Layer: " + newTag);
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
