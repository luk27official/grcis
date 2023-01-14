using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MathSupport;
using OpenglSupport;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TexLib;

namespace _039terrain
{
  public partial class Form1
  {
    /// <summary>
    /// Optional data initialization.
    /// </summary>
    static void InitParams (out int iterations, out float roughness, out string param, out string tooltip, out string name)
    {
      name       = "Lukáš Polák";
      iterations = 0;
      roughness  = 0.2f;
      param      = "";
      tooltip    = "tooltip";
    }

    #region GPU data

    /// <summary>
    /// Texture identifier (for one texture only, extend the source code if necessary)
    /// </summary>
    private int textureId = 0;

    private uint[] VBOid = new uint[2];   // [0] .. vertex array, [1] .. index buffer

    // vertex-buffer offsets:
    private int textureCoordOffset = 0;
    private int colorOffset        = 0;
    private int normalOffset       = 0;
    private int vertexOffset       = 0;
    private int stride             = 0;

    #endregion

    #region Lighting data

    // light:
    float[] ambientColor  = {0.1f, 0.1f, 0.1f};
    float[] diffuseColor  = {1.0f, 1.0f, 1.0f};
    float[] specularColor = {1.0f, 1.0f, 1.0f};
    float[] lightPosition = {1.0f, 1.0f, 0.0f};

    // material:
    float[] materialAmbient  = {0.1f, 0.1f, 0.1f};
    float[] materialDiffuse  = {0.8f, 0.8f, 0.8f};
    float[] materialSpecular = {0.5f, 0.5f, 0.5f};
    float  materialShininess = 60.0f;

    /// <summary>
    /// Current light position.
    /// </summary>
    Vector4 lightPos = Vector4.UnitY * 4.0f;

    /// <summary>
    /// Current light angle in radians.
    /// </summary>
    double lightAngle = 0.0;

    #endregion

    /// <summary>
    /// OpenGL init code.
    /// </summary>
    void InitOpenGL ()
    {
      // log OpenGL info just for curiosity:
      GlInfo.LogGLProperties();

      // OpenGL init code:
      glControl1.VSync = true;
      GL.ClearColor(Color.Black);
      GL.Enable(EnableCap.DepthTest);

      // VBO init:
      GL.GenBuffers(2, VBOid); // two buffers, one for vertex data, one for index data
      if (GL.GetError() != ErrorCode.NoError)
        throw new Exception("Couldn't create VBOs");

      GL.Light(LightName.Light0, LightParameter.Ambient,  ambientColor);
      GL.Light(LightName.Light0, LightParameter.Diffuse,  diffuseColor);
      GL.Light(LightName.Light0, LightParameter.Specular, specularColor);
    }

    private void glControl1_Load (object sender, EventArgs e)
    {
      // cold start
      InitOpenGL();

      // warm start
      SetupViewport();

      // initialize the scene
      int iterations  = (int)upDownIterations.Value;
      float roughness = (float)upDownRoughness.Value;
      Regenerate(iterations, roughness, textParam.Text);
      labelStatus.Text = "Triangles: " + scene.Triangles;

      // initialize the simulation
      InitSimulation(true);

      loaded = true;
      Application.Idle += new EventHandler(Application_Idle);
    }

    /// <summary>
    /// This class stores information about the vectors which are drawn.
    /// </summary>
    private class CustomVector
    {
      public Vector3 vect;  //vector coords
      public int id;        //id of the vector in OpenGL
      public bool rendered; //true if the vector should be rendered in OpenGL

      public CustomVector()
      {
        this.rendered = true;
      }
    }

    static Random rnd = new Random();

    double[][] heightMap;   //an 2D array containing all the heights
    CustomVector[][] grid;  //an 2D array containing the vectors to be drawn
    Vector3[][] normals;    //an 2D array containing information about the normals

    bool precomputed = false; //true if the heightmap was not generated but loaded from a file

    float roughLast;    //stores the last roughness value
    int iterationsLast; //stores the last iterations value
    string paramsLast;  //stores the last param value

    /// <summary>
    /// This method returns a random number from a given range.
    /// </summary>
    /// <param name="min">Minimal value</param>
    /// <param name="max">Maximal value</param>
    /// <returns>A random number from a given range</returns>
    private static float RandomInRange(float min, float max)
    {
      return (float)(rnd.NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// This method generates the heights using the diamond square method.
    /// </summary>
    /// <param name="tiles">Grid of doubles to store the heights.</param>
    /// <param name="roughness">Roughness parameter (0 to 5).</param>
    /// <param name="worldSize">World size.</param>
    private void GenerateHeightMap (double[][] tiles, float roughness, int worldSize)
    {
      if(!precomputed)
      {
        tiles[0][0] = tiles[0][worldSize - 1] = tiles[worldSize - 1][worldSize - 1] = 0;
      }
      double normalizedRoughness = roughness / 5;
      for (int sideLength = worldSize - 1; sideLength >= 2; sideLength /= 2, normalizedRoughness /= 2.0)
      {
        int halfSide = sideLength / 2;

        for (int x = 0; x < worldSize - 1; x += sideLength)
        {
          for (int y = 0; y < worldSize - 1; y += sideLength)
          {
            if(!Geometry.IsZero(tiles[x + halfSide][y + halfSide]))
            {
              //because we do not know the roughness param of the precomputed heightmap, we will re-calculate the edges...
              if (precomputed && x != 0 && y != 0 && x != worldSize - 1 && y != worldSize - 1)
              {
                continue;
              }
              else if (!precomputed)
              {
                continue;
              }
            }

            double avg = tiles[x][y] + tiles[x + sideLength][y] + tiles[x][y + sideLength] + tiles[x + sideLength][y + sideLength];
            avg /= 4.0;

            tiles[x + halfSide][y + halfSide] = (float)(avg + (rnd.NextDouble() * 2 * normalizedRoughness) - normalizedRoughness);
          }
        }

        for (int x = 0; x < worldSize - 1; x += halfSide)
        {
          for (int y = (x + halfSide) % sideLength; y < worldSize - 1; y += sideLength)
          {
            if (!Geometry.IsZero(tiles[x][y]))
            {
              //because we do not know the roughness param of the precomputed heightmap, we will re-calculate the edges...
              if (precomputed && x != 0 && y != 0 && x != worldSize - 1 && y != worldSize - 1)
              {
                continue;
              }
              else if (!precomputed)
              {
                continue;
              }
            }

            double avg =
              tiles[(x - halfSide + worldSize - 1) % (worldSize - 1)][y] + //left
					    tiles[(x + halfSide) % (worldSize - 1)][y] + //right
					    tiles[x][(y + halfSide) % (worldSize - 1)] + //below
					    tiles[x][(y - halfSide + worldSize - 1) % (worldSize - 1)]; //above

            avg /= 4.0;

            avg = (float)(avg + (rnd.NextDouble() * 2 * normalizedRoughness) - normalizedRoughness);

            tiles[x][y] = avg;

            if (x == 0)
            {
              tiles[worldSize - 1][y] = avg;
            }
            if (y == 0)
            {
              tiles[x][worldSize - 1] = avg;
            }
          }
        }
      }
    }

    /// <summary>
    /// Returns the height of the point [x,y].
    /// </summary>
    /// <param name="x">x-coord</param>
    /// <param name="y">y-coord</param>
    /// <returns>The height.</returns>
    private float ReturnHeightMapValue(int x, int y)
    {
      return (float)heightMap[x][y];
    }

    /// <summary>
    /// This method returns the vector length.
    /// </summary>
    /// <param name="vector">The vector</param>
    /// <returns>The vector length</returns>
    public static float GetVectorLength(Vector3 vector)
    {
      float length = (float)Math.Sqrt(Math.Pow(vector.X, 2) + Math.Pow(vector.Y, 2) + Math.Pow(vector.Z, 2));
      return length;
    }

    /// <summary>
    /// This static method computes a normalized vector (the length of the vector is 1).
    /// </summary>
    /// <param name="vector">Vector to be normalized</param>
    /// <returns>The normalized vector</returns>
    public static Vector3 NormalizeVector(Vector3 vector)
    {
      float length = GetVectorLength(vector);
      Vector3 normalizedVector = new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
      return normalizedVector;
    }

    /// <summary>
    /// This method computes the normals for given vertices.
    /// </summary>
    /// <returns>An 2D array with the normals.</returns>
    private Vector3[][] ComputeNormals()
    {
      Vector3[][] normals = new Vector3[grid.Length][];

      for (int y = 0; y < grid.Length; y++)
      {
        normals[y] = new Vector3[grid.Length];
        for (int x = 0; x < grid.Length; x++)
        {
          float sx = ReturnHeightMapValue(x < grid.Length-1 ? x+1 : x, y) - ReturnHeightMapValue(x > 0 ? x-1 : x, y);
          if (x == 0 || x == grid.Length - 1)
          {
            sx *= 2;
          }

          float sy = ReturnHeightMapValue(x, y < grid.Length-1 ? y+1 : y) - ReturnHeightMapValue(x, y > 0 ?  y-1 : y);
          if (y == 0 || y == grid.Length - 1)
          {
            sy *= 2;
          }

          normals[y][x] = new Vector3(-sx, 2.0f, sy);
          normals[y][x] = NormalizeVector(normals[y][x]);
        }
      }
      return normals;
    }

    /// <summary>
    /// This method generates a new grid of vertices with some random height values (those should be changed later in the code).
    /// </summary>
    /// <param name="size">Size of the grid (one side)</param>
    /// <param name="minValue">Minimal x/y-value</param>
    /// <param name="maxValue">Maximal x/y-value</param>
    private void GenerateNewGrid (int size, double minValue, double maxValue)
    {
      double xVal, zVal;
      grid = new CustomVector[size][];
      for (int i = 1; i <= size; i++)
      {
        grid[i - 1] = new CustomVector[size];
        xVal = minValue + (maxValue - minValue) * (i - 1) / (size - 1);

        for (int j = 1; j <= size; j++)
        {
          zVal = minValue + (maxValue - minValue) * (j - 1) / (size - 1);
          grid[i - 1][j - 1] = new CustomVector();
          grid[i - 1][j - 1].vect = new Vector3((float)xVal, (float)rnd.NextDouble() / 4, (float)zVal);
        }
      }
    }

    /// <summary>
    /// [Re-]generate the terrain data.
    /// </summary>
    /// <param name="iterations">Number of subdivision iteratons.</param>
    /// <param name="roughness">Roughness parameter.</param>
    /// <param name="param">Optional text parameter.</param>
    private void Regenerate (int iterations, float roughness, string param)
    {
      bool forceRegenerate = false;
      
      if(iterations == iterationsLast && param == paramsLast)
      {
        forceRegenerate = true;
      }

      scene.Reset();

      int size = (int)Math.Pow(2, iterations) + 1;

      double minValue = -0.5;
      double maxValue = 0.5;

      int skip = -1; //how many vertices will be skipped

      #region gridLogic
      if (!forceRegenerate && grid != null && grid.Length > size) //less vertices are shown
      {
        int formerSize = (int)Math.Log(heightMap.Length - 1, 2);

        skip = (int)Math.Pow(2, formerSize - (int)Math.Log(size - 1, 2)); 
        //every skip-th element will be skipped
        for (int i = 0; i < grid.Length; i++)
        {
          for(int j = 0; j < grid.Length; j++)
          {
            if(i % skip == 0 && j % skip == 0)
            {
              grid[i][j].rendered = true;
            }
            else
            {
              grid[i][j].rendered = false;
            } 
          }
        }
      }
      else if ((grid != null && grid.Length < size) || (grid == null || forceRegenerate)) //more vertices should be shown or there is need to regenerate the grid
      {
        GenerateNewGrid(size, minValue, maxValue);
      }
      #endregion

      #region customHeightMap
      /*
       * To enable the custom heightmap, type the name of the file to the params text field.
       * Then hit the "regenerate" button. 
       * When increasing the heightmap iterations, the program cannot guess the roughness parameter.
       * The edges thus might be a little fuzzy (I tried to reduce it as much as possible, but it is not perfect).
       * If the user wants a "perfect" representation of the heightmap, they should scale the bitmap image
       * to the preferred size (like scaling 33x33 to 129x129).
       * 
       * WARNINGS!
       * The heightmap has to be of a size 2^n + 1 !
       * The heightmap should be grayscale (the heights are computed by the red shade) !
       * When loading the heightmap, the iterations number has to be lower than the bitmap resolution!
       * 
       * In other cases, the program ignores the custom heightmap parameter.
       */
      if (param.Length > 0 && paramsLast != param)
      {
        try
        {
          Bitmap bmp = new Bitmap(param);
          //check bmp size 2^n+1, otherwise reject
          //it is also better to reject if the iterations number is higher than the bmp size (ideally the user should enter the corresponding size)
          if(Math.Log(bmp.Width - 1, 2) != Math.Floor(Math.Log(bmp.Width - 1, 2)) || Math.Log(bmp.Width - 1, 2) < iterations)
          {
            throw new Exception("Invalid heightmap size");
          }

          precomputed = true;

          GenerateNewGrid(bmp.Width, minValue, maxValue);

          double[][] newhm = new double[bmp.Width][];

          //for each pixel get the color and set the corresponding height
          for (int i = 0; i < bmp.Width; i++)
          {
            newhm[i] = new double[bmp.Width];
            for(int j = 0; j < bmp.Width; j++)
            {
              Color c = bmp.GetPixel(i, j);
              //0.4f here is an "optimal" constant, the user may remove it but then more water/hilly surface will appear
              float color = 1 - (c.G / (float)255) - 0.4f;
              newhm[i][j] = color;
            }
          }

          heightMap = newhm;
        }
        catch (Exception e)
        {
          precomputed = false;
          Debug.WriteLine(e.Message);
          GenerateHeightMap(heightMap, roughness, size);
        }
        finally
        {
          normals = ComputeNormals();
        }
      }
      #endregion

      #region heightMapLogic
      if (!forceRegenerate && heightMap != null && heightMap.Length > size) //we need to show less vertices
      {
        // we do not need to do anything here right now
      }
      else if (heightMap != null && heightMap.Length < size) //we need to show more vertices - compute more heights
      {
        double[][] temp = heightMap;
        heightMap = new double[size][];
        for(int i = 0; i < temp.Length; i++)
        {
          heightMap[i*2] = new double[size];
          for(int j = 0; j <= size / 2; j++)
          {
            heightMap[i * 2][j * 2] = temp[i][j];
          }
          if (i > 0)
          {
            heightMap[i * 2 - 1] = new double[size];
          }
        }
        GenerateHeightMap(heightMap, roughness, size);
        normals = ComputeNormals();
      }
      else if (heightMap == null || forceRegenerate) //else if regenerating for the first time / force regenerating
      {
        precomputed = false;
        heightMap = new double[size][];
        for (int i = 0; i < grid.Length; i++)
        {
          heightMap[i] = new double[size];
        }
        GenerateHeightMap(heightMap, roughness, size);
        normals = ComputeNormals();
      }
      #endregion

      #region verticesLogic
      //create all of the vertices and the normals and color them
      for (int i = 0; i < grid.Length; i++)
      {
        for (int j = 0; j < grid.Length; j++)
        {
          grid[i][j].vect.Y = (float)heightMap[i][j] / 2.5f;

          grid[i][j].id = scene.AddVertex(grid[i][j].vect);

          scene.SetNormal(grid[i][j].id, normals[i][j]);

          if((float)heightMap[i][j] <= -0.3) //deep water
          {
            scene.SetColor(grid[i][j].id, new Vector3(0.0f, RandomInRange(0.03f, 0.04f), RandomInRange(0.35f, 0.45f)));
          }
          else if ((float)heightMap[i][j] <= -0.1) //normal water
          {
            //0.00, 0.0683, 0.820
            //0.00, 0.511, 0.930
            scene.SetColor(grid[i][j].id, new Vector3(0.0f, RandomInRange(0.1f, 0.4f), RandomInRange(0.8f, 0.9f)));
          }
          else if ((float)heightMap[i][j] <= -0.05) //sand
          {
            //0.890, 0.724, 0.125
            //0.960, 0.807, 0.125
            scene.SetColor(grid[i][j].id, new Vector3(RandomInRange(0.85f, 0.95f), RandomInRange(0.7f, 0.8f), 0.125f));
          }
          else if ((float)heightMap[i][j] <= 0.25) //grass
          {
            //0.0216, 0.540, 0.125
            //0.166, 0.790, 0.291
            scene.SetColor(grid[i][j].id, new Vector3(RandomInRange(0.05f, 0.15f), RandomInRange(0.55f, 0.75f), RandomInRange(0.15f, 0.25f)));
          }
          else if ((float)heightMap[i][j] <= 0.7) //mountain soil
          {
            //0.910, 0.806, 0.0182
            //0.370, 0.329, 0.0148
            scene.SetColor(grid[i][j].id, new Vector3(RandomInRange(0.6f, 0.8f), RandomInRange(0.4f, 0.6f), RandomInRange(0.01f, 0.02f)));
          }
          else //snow
          {
            //0.970, 0.970, 0.970
            //0.890, 0.890, 0.890
            scene.SetColor(grid[i][j].id, new Vector3(RandomInRange(0.9f, 0.97f), RandomInRange(0.9f, 0.97f), RandomInRange(0.9f, 0.97f)));
          }
        }
      }
      #endregion

      #region triangleLogic
      //create all the triangles
      for (int i = 0; i < grid.Length; i++)
      {
        for(int j = 0; j < grid.Length; j++)
        {
          //Debug.WriteLine("X: " + grid[i][j].vect.X + ", Y: " + grid[i][j].vect.Y + ", Z: " + grid[i][j].vect.Z + ", id: " + grid[i][j].id);

          if (skip != -1)
          {
            if(j % skip == 0 && i % skip == 0)
            {
              if ((j < grid.Length - skip) && (i < grid.Length - skip))
              {
                scene.AddTriangle(grid[i][j + skip].id, grid[i + skip][j].id, grid[i][j].id);
              }
              if (j >= skip && i >= skip)
              {
                scene.AddTriangle(grid[i][j].id, grid[i - skip][j].id, grid[i][j - skip].id);
              }
            }
          }
          else
          {
            if ((j < grid.Length - 1) && (i < grid.Length - 1))
            {
              scene.AddTriangle(grid[i][j + 1].id, grid[i + 1][j].id, grid[i][j].id);
            }
            if (j > 0 && i > 0)
            {
              scene.AddTriangle(grid[i][j].id, grid[i - 1][j].id, grid[i][j - 1].id);
            }
          }
        }
        #endregion

        iterationsLast = iterations;
        roughLast = roughness;
        paramsLast = param;
      }

      // this function uploads the data to the graphics card
      PrepareData();

      // simulation / hovercraft [re]initialization?
      InitSimulation(false);
    }

    /// <summary>
    /// last simulated time in seconds.
    /// </summary>
    double simTime = 0.0;

    /// <summary>
    /// Are we doing the terrain-flyover?
    /// </summary>
    bool hovercraft = false;

    /// <summary>
    /// Init of animation / hovercraft simulation, ...
    /// </summary>
    /// <param name="cold">True for global reset (including light-source/vehicle position..)</param>
    private void InitSimulation (bool cold)
    {
      if (hovercraft)
      {
        // !!! TODO: hovercraft init
      }
      else
        if (cold)
        {
          lightPos = new Vector4(lightPosition[0], lightPosition[1], lightPosition[2], 1.0f);
          lightAngle = 0.0;
        }

      long nowTicks = DateTime.Now.Ticks;
      simTime = nowTicks* 1.0e-7;
    }

    private void glControl1_Paint (object sender, PaintEventArgs e)
    {
      if (checkAnim.Checked)
        Simulate(DateTime.Now.Ticks * 1.0e-7);

      Render();
    }

    /// <summary>
    /// One step of animation / hovercraft simulation.
    /// </summary>
    /// <param name="time"></param>
    private void Simulate (double time)
    {
      if (!loaded ||
          time <= simTime)
        return;

      double dt = time - simTime;   // delta-time in seconds

      if (hovercraft)
      {
        // !!! TODO: hovercraft simulation
      }

      lightAngle += dt;             // one radian per second..
      Matrix4 m;
      Matrix4.CreateRotationY((float)lightAngle, out m);
      lightPos = Vector4.Transform(m, new Vector4(lightPosition[0], lightPosition[1], lightPosition[2], 1.0f));

      simTime = time;
    }

    /// <summary>
    /// Rendering of one frame.
    /// </summary>
    private void Render ()
    {
      if (!loaded) return;

      frameCounter++;

      // frame init:
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      GL.ShadeModel(checkSmooth.Checked ? ShadingModel.Smooth : ShadingModel.Flat);
      GL.PolygonMode(MaterialFace.FrontAndBack,
                     checkWireframe.Checked ? PolygonMode.Line : PolygonMode.Fill);

      // camera:
      SetCamera();

      // OpenGL lighting:
      GL.MatrixMode(MatrixMode.Modelview);
      GL.Light(LightName.Light0, LightParameter.Position, lightPos);
      GL.Enable(EnableCap.Light0);
      GL.Enable(EnableCap.Lighting);

      // texturing:
      bool useTexture = scene.HasTxtCoords() &&
                        checkTexture.Checked &&
                        textureId > 0;
      if (useTexture)
      {
        // set up the texture:
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.Enable(EnableCap.Texture2D);
      }
      else
      {
        GL.Disable(EnableCap.Texture2D);

        GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.AmbientAndDiffuse);
        GL.Enable(EnableCap.ColorMaterial);
      }

      // common lighting colors/parameters:
      GL.Material(MaterialFace.Front, MaterialParameter.Ambient,   materialAmbient);
      GL.Material(MaterialFace.Front, MaterialParameter.Diffuse,   materialDiffuse);
      GL.Material(MaterialFace.Front, MaterialParameter.Specular,  materialSpecular);
      GL.Material(MaterialFace.Front, MaterialParameter.Shininess, materialShininess);

      // scene -> vertex buffer & index buffer

      // bind the vertex buffer:
      GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[0]);

      // tell OGL what sort of data we have and where in the buffer they could be found
      // the buffers we get from SceneBrep are interleaved => stride != 0
      if (useTexture)
        GL.TexCoordPointer(2, TexCoordPointerType.Float, stride, textureCoordOffset);

      if (scene.HasColors())
        GL.ColorPointer(3, ColorPointerType.Float, stride, colorOffset);

      if (scene.HasNormals())
        GL.NormalPointer(NormalPointerType.Float, stride, normalOffset);

      GL.VertexPointer(3, VertexPointerType.Float, stride, vertexOffset);

      // bind the index buffer:
      GL.BindBuffer(BufferTarget.ElementArrayBuffer, VBOid[1]);

      // draw the geometry:
      triangleCounter += scene.Triangles;
      GL.DrawElements(BeginMode.Triangles, scene.Triangles * 3, DrawElementsType.UnsignedInt, 0);

      if (useTexture)
        GL.BindTexture(TextureTarget.Texture2D, 0);
      else
        GL.Disable(EnableCap.ColorMaterial);
      GL.Disable(EnableCap.Light0);
      GL.Disable(EnableCap.Lighting);

      // light-source rendering (small white rectangle):
      GL.PointSize(3.0f);
      GL.Begin(PrimitiveType.Points);
      GL.Color3(1.0f, 1.0f, 1.0f);
      GL.Vertex4(lightPos);
      GL.End();

      // swap buffers:
      glControl1.SwapBuffers();
    }

    #region Camera attributes

    /// <summary>
    /// Current "up" vector.
    /// </summary>
    private Vector3 up = Vector3.UnitY;

    /// <summary>
    /// Vertical field-of-view angle in radians.
    /// </summary>
    private float fov = 1.0f;

    /// <summary>
    /// Camera's near point.
    /// </summary>
    private float near = 0.1f;

    /// <summary>
    /// Camera's far point.
    /// </summary>
    private float far = 200.0f;

    /// <summary>
    /// Current elevation angle in radians.
    /// </summary>
    private double elevationAngle = 0.1;

    /// <summary>
    /// Current azimuth angle in radians.
    /// </summary>
    private double azimuthAngle = 0.0;

    /// <summary>
    /// Current zoom factor.
    /// </summary>
    private double zoom = 2.0;

    #endregion

    /// <summary>
    /// Function called whenever the main application is idle..
    /// </summary>
    private void Application_Idle (object sender, EventArgs e)
    {
      while (glControl1.IsIdle)
      {
        glControl1.Invalidate();                // causes the GLcontrol 'repaint' action

        long now = DateTime.Now.Ticks;
        if (now - lastFpsTime > 5000000)        // more than 0.5 sec
        {
          lastFps = 0.5 * lastFps + 0.5 * (frameCounter    * 1.0e7 / (now - lastFpsTime));
          lastTps = 0.5 * lastTps + 0.5 * (triangleCounter * 1.0e7 / (now - lastFpsTime));
          lastFpsTime = now;
          frameCounter = 0;
          triangleCounter = 0L;

          if (lastTps < 5.0e5)
            labelFps.Text = string.Format(CultureInfo.InvariantCulture, "Fps: {0:f1}, tps: {1:f0}k",
                                          lastFps, (lastTps * 1.0e-3));
          else
            labelFps.Text = string.Format(CultureInfo.InvariantCulture, "Fps: {0:f1}, tps: {1:f1}m",
                                          lastFps, (lastTps * 1.0e-6));
        }
      }
    }

    /// <summary>
    /// Called in case the GLcontrol geometry changes.
    /// </summary>
    private void SetupViewport ()
    {
      int width  = glControl1.Width;
      int height = glControl1.Height;

      // 1. set ViewPort transform:
      GL.Viewport(0, 0, width, height);

      // 2. set projection matrix
      GL.MatrixMode(MatrixMode.Projection);
      Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(fov, width / (float)height, near, far);
      GL.LoadMatrix(ref proj);
    }

    private void ResetCamera ()
    {
      elevationAngle = 0.1;
      azimuthAngle = 0.0;
      zoom = 2.0;
    }

    /// <summary>
    /// Camera setup, called for every frame prior to any rendering.
    /// </summary>
    private void SetCamera ()
    {
      if (hovercraft)
      {
        // !!! TODO: hovercraft camera
      }
      else
      {
        Vector3 cameraPosition = new Vector3(0.0f, 0, (float)zoom);

        Matrix4 rotateX = Matrix4.CreateRotationX((float)-elevationAngle);
        Matrix4 rotateY = Matrix4.CreateRotationY((float)azimuthAngle);

        cameraPosition = Vector3.TransformPosition(cameraPosition, rotateX);
        cameraPosition = Vector3.TransformPosition(cameraPosition, rotateY);

        GL.MatrixMode(MatrixMode.Modelview);
        Matrix4 lookAt = Matrix4.LookAt(cameraPosition, Vector3.Zero, up);

        GL.LoadMatrix(ref lookAt);
      }
    }

    /// <summary>
    /// Prepare VBO content and upload it to the GPU.
    /// You probably don't need to change this function..
    /// </summary>
    private void PrepareData ()
    {
      Debug.Assert(scene != null, "Missing scene");

      if (scene.Triangles == 0)
        return;

      // enable the respective client states
      GL.EnableClientState(ArrayCap.VertexArray);   // vertex array (positions?)

      if (scene.HasColors())                        // colors, if any
        GL.EnableClientState(ArrayCap.ColorArray);

      if (scene.HasNormals())                       // normals, if any
        GL.EnableClientState(ArrayCap.NormalArray);

      if (scene.HasTxtCoords())                     // textures, if any
        GL.EnableClientState(ArrayCap.TextureCoordArray);

      // bind the vertex array (interleaved)
      GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[0]);

      // query the size of the buffer in bytes
      int vertexBufferSize = scene.VertexBufferSize(
          true, // we always have vertex data
          scene.HasTxtCoords(),
          scene.HasColors(),
          scene.HasNormals());

      // fill vertexData with data we will upload to the (vertex) buffer on the graphics card
      float[] vertexData = new float[vertexBufferSize / sizeof(float)];

      // calculate the offsets in the interleaved array
      textureCoordOffset = 0;
      colorOffset = textureCoordOffset + scene.TxtCoordsBytes();
      normalOffset = colorOffset + scene.ColorBytes();
      vertexOffset = normalOffset + scene.NormalBytes();

      // convert data from SceneBrep to float[] (interleaved array)
      unsafe
      {
        fixed (float* fixedVertexData = vertexData)
        {
          stride = scene.FillVertexBuffer(
              fixedVertexData,
              true,
              scene.HasTxtCoords(),
              scene.HasColors(),
              scene.HasNormals());

          // upload vertex data to the graphics card
          GL.BufferData(
              BufferTarget.ArrayBuffer,
              (IntPtr)vertexBufferSize,
              (IntPtr)fixedVertexData,        // still pinned down to fixed address..
              BufferUsageHint.StaticDraw);
        }
      }

      // index buffer:
      GL.BindBuffer(BufferTarget.ElementArrayBuffer, VBOid[1]);

      // convert indices from SceneBrep to uint[]
      uint[] indexData = new uint[scene.Triangles * 3];

      unsafe
      {
        fixed (uint* unsafeIndexData = indexData)
        {
          scene.FillIndexBuffer(unsafeIndexData);

          // upload index data to video memory
          GL.BufferData(
              BufferTarget.ElementArrayBuffer,
              (IntPtr)(scene.Triangles * 3 * sizeof(uint)),
              (IntPtr)unsafeIndexData,        // still pinned down to fixed address..
              BufferUsageHint.StaticDraw);
        }
      }
    }

    private void glControl1_KeyDown (object sender, KeyEventArgs e)
    {
      // !!!{{ TODO: add the event handler here
      // !!!}}
    }

    private void glControl1_KeyUp (object sender, KeyEventArgs e)
    {
      // !!!{{ TODO: add the event handler here
      // !!!}}
    }

    private int dragFromX = 0;
    private int dragFromY = 0;
    private bool dragging = false;

    private void glControl1_MouseDown (object sender, MouseEventArgs e)
    {
      if (hovercraft)
      {
        // !!! TODO: hovercraft
      }
      else
      {
        dragFromX = e.X;
        dragFromY = e.Y;
        dragging = true;
      }
    }

    private void glControl1_MouseUp (object sender, MouseEventArgs e)
    {
      if (hovercraft)
      {
        // !!! TODO: hovercraft
      }
      else
      {
        dragging = false;
      }
    }

    private void glControl1_MouseMove (object sender, MouseEventArgs e)
    {
      if (hovercraft)
      {
        // !!! TODO: hovercraft
      }
      else
      {
        if (!dragging) return;

        int delta;
        if (e.X != dragFromX)       // change the azimuth angle
        {
          delta = e.X - dragFromX;
          dragFromX = e.X;
          azimuthAngle -= delta * 4.0 / glControl1.Width;
        }

        if (e.Y != dragFromY)       // change the elevation angle
        {
          delta = e.Y - dragFromY;
          dragFromY = e.Y;
          elevationAngle += delta * 2.0 / glControl1.Height;
          elevationAngle = Arith.Clamp(elevationAngle, -1.0, 1.5);
        }
      }
    }

    private void glControl1_MouseWheel (object sender, MouseEventArgs e)
    {
      if (e.Delta != 0)
        if (hovercraft)
        {
          // !!! TODO: hovercraft
        }
        else
        {
          float change = e.Delta / 120.0f;
          zoom = Arith.Clamp(zoom * Math.Pow(1.05, change), 0.5, 100.0);
        }
    }
  }
}
