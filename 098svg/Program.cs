using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MathSupport;
using OpenTK;
using Utilities;

namespace _098svg
{
  enum DrawWall
  {
    LEFT = 0, 
    TOP = 1, 
    RIGHT = 2, 
    BOTTOM = 3
  }

  class Cell
  {
    public int X {get; private set;}
    public int Y {get; private set;}
    public bool Visited {get; set;}
    public bool[] walls = new bool[4];

    public Cell (int y, int x)
    {
      this.X = x;
      this.Y = y;
      for (int i = 0; i < 4; i++)
      {
        walls[i] = true;
      }
    }
  }

  class Maze
  {
    private int height, width;
    private Cell[,] cells;
    private RandomJames rnd;

    public Maze (int height, int width, RandomJames rnd)
    {
      this.height = height;
      this.width = width;
      cells = new Cell[height, width];
      for (int i = 0; i < height; i++)
      {
        for (int j = 0; j < width; j++)
        {
          cells[i, j] = new Cell(i, j);
        }
      }
      this.rnd = rnd;
    }

    /// <summary>
    /// Writes the maze in a SVG format to the writer.
    /// </summary>
    /// <param name="writer">Provided writer.</param>
    public void WriteToSVG(StreamWriter writer)
    {
      IEnumerable<DrawWall> values = Enum.GetValues(typeof(DrawWall)).Cast<DrawWall>();

      int size = CmdOptions.options.penSize;

      for (int i = 0; i < height; i++)
      {
        for(int j = 0; j < width; j++)
        {
          foreach (DrawWall direction in values)
          {
            if(cells[i, j].walls[(int)direction])
            {
              Program.DrawLine(writer, j * -size, i * -size, size, direction, 0, 0, 0);
              //it may seem weird that it is multiplied by -1, but the DrawLine method subtracts from x and y, so it is actually adding
            }
          }
        }
      }
    }

    /// <summary>
    /// A method returning information whether there exists an unvisited cell.
    /// </summary>
    /// <returns>True if there is at least one unvisited cell.</returns>
    private bool ExistsUnvisitedCell()
    {
      for (int i = 0; i < height; i++)
      {
        for (int j = 0; j < width; j++)
        {
          if (!cells[i, j].Visited)
            return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Method checking an existence of an cell (due to boundaries).
    /// </summary>
    /// <param name="x">X coord</param>
    /// <param name="y">Y coord</param>
    /// <returns>True if the coordinates fit the boundaries.</returns>
    private bool Exists(int x, int y)
    {
      if (x < 0 || y < 0 || x > width - 1 || y > height - 1)
        return false;
      return true;
    }

    /// <summary>
    /// Method resetting the visited value of every cell.
    /// </summary>
    private void ResetVisited()
    {
      for (int i = 0; i < height; i++)
      {
        for (int j = 0; j < width; j++)
        {
          cells[i, j].Visited = false;
        }
      }
    }

    /// <summary>
    /// BFS for solving the maze.
    /// </summary>
    /// <param name="x">X coord of the first point</param>
    /// <param name="y">Y coord of the first point</param>
    /// <param name="x2">X coord of the second point</param>
    /// <param name="y2">Y coord of the second point</param>
    /// <returns>Depth of the maze (length of the shortest path).</returns>
    public int? FindShortestPath(int x, int y, int x2, int y2) { 
      ResetVisited();
      Queue<Cell> queue = new Queue<Cell>();
      queue.Enqueue(cells[y, x]);
      cells[y, x].Visited = true;
      int count = 0;
      int depth = 0;
      while (queue.Count > 0)
      {
        int queue_size = queue.Count;
        while(queue_size-- != 0) {
          Cell cell = queue.Dequeue();
          if (cell.X == x2 && cell.Y == y2)
            {
              //Console.WriteLine("Depth: {0}, count: {1}", depth, count);
              return depth;
            }

          if (Exists(cell.X, cell.Y - 1) && !cells[cell.Y - 1, cell.X].Visited && !cells[cell.Y, cell.X].walls[(int)DrawWall.TOP])
          {
            cells[cell.Y - 1, cell.X].Visited = true;
            queue.Enqueue(cells[cell.Y - 1, cell.X]);
          }

          if (Exists(cell.X, cell.Y + 1) && !cells[cell.Y + 1, cell.X].Visited && !cells[cell.Y, cell.X].walls[(int)DrawWall.BOTTOM])
          {
            cells[cell.Y + 1, cell.X].Visited = true;
            queue.Enqueue(cells[cell.Y + 1, cell.X]);
          }

          if (Exists(cell.X - 1, cell.Y) && !cells[cell.Y, cell.X - 1].Visited && !cells[cell.Y, cell.X].walls[(int)DrawWall.LEFT])
          {
            cells[cell.Y, cell.X - 1].Visited = true;
            queue.Enqueue(cells[cell.Y, cell.X - 1]);
          }

          if (Exists(cell.X + 1, cell.Y) && !cells[cell.Y, cell.X + 1].Visited && !cells[cell.Y, cell.X].walls[(int)DrawWall.RIGHT])
          {
            cells[cell.Y, cell.X + 1].Visited = true;
            queue.Enqueue(cells[cell.Y, cell.X + 1]);
          }
          count++;
        }
        depth++;
      }
      return null; //path not found - should not happen
    }

    /// <summary>
    /// Method which generates the maze.
    /// </summary>
    public void Generate()
    {
      //pick a random starting cell
      Vector2d vect = new Vector2d
      {
        X = rnd.RandomInteger(0, width - 1),
        Y = rnd.RandomInteger(0, height - 1),
      };

      this.cells[(int)vect.Y, (int)vect.X].Visited = true;

      Stack<Vector2d> path = new Stack<Vector2d>();
      path.Push(vect);

      while(ExistsUnvisitedCell())
      {
        Vector2d newVect = path.Peek();

        List<DrawWall> list = new List<DrawWall>();

        if (Exists((int)newVect.X, (int)newVect.Y - 1))
        {
          if(!cells[(int)newVect.Y - 1, (int)newVect.X].Visited)
          {
            list.Add(DrawWall.TOP);
          }
        }

        if (Exists((int)newVect.X, (int)newVect.Y + 1))
        {
          if (!cells[(int)newVect.Y + 1, (int)newVect.X].Visited)
          {
            list.Add(DrawWall.BOTTOM);
          }
        }

        if (Exists((int)newVect.X + 1, (int)newVect.Y))
        {
          if (!cells[(int)newVect.Y, (int)newVect.X + 1].Visited)
          {
            list.Add(DrawWall.RIGHT);
          }
        }

        if (Exists((int)newVect.X - 1, (int)newVect.Y))
        {
          if (!cells[(int)newVect.Y, (int)newVect.X - 1].Visited)
          {
            list.Add(DrawWall.LEFT);
          }
        }

        if(list.Count > 0)
        {
          int newDir = rnd.RandomInteger(0, list.Count() - 1);
          DrawWall dd = list[newDir];

          switch (dd)
          {
             case DrawWall.TOP:
              cells[(int)newVect.Y, (int)newVect.X].walls[(int)DrawWall.TOP] = false;
              cells[(int)newVect.Y - 1, (int)newVect.X].walls[(int)DrawWall.BOTTOM] = false;
              cells[(int)newVect.Y - 1, (int)newVect.X].Visited = true;
              path.Push(new Vector2d { X = newVect.X, Y = newVect.Y - 1 });
              break;
             case DrawWall.BOTTOM:
              cells[(int)newVect.Y, (int)newVect.X].walls[(int)DrawWall.BOTTOM] = false;
              cells[(int)newVect.Y + 1, (int)newVect.X].walls[(int)DrawWall.TOP] = false;
              cells[(int)newVect.Y + 1, (int)newVect.X].Visited = true;
              path.Push(new Vector2d { X = newVect.X, Y = newVect.Y + 1 });
              break;
             case DrawWall.RIGHT:
              cells[(int)newVect.Y, (int)newVect.X].walls[(int)DrawWall.RIGHT] = false;
              cells[(int)newVect.Y, (int)newVect.X + 1].walls[(int)DrawWall.LEFT] = false;
              cells[(int)newVect.Y, (int)newVect.X + 1].Visited = true;
              path.Push(new Vector2d { X = newVect.X + 1, Y = newVect.Y });
              break;
            case DrawWall.LEFT:
              cells[(int)newVect.Y, (int)newVect.X].walls[(int)DrawWall.LEFT] = false;
              cells[(int)newVect.Y, (int)newVect.X - 1].walls[(int)DrawWall.RIGHT] = false;
              cells[(int)newVect.Y, (int)newVect.X - 1].Visited = true;
              path.Push(new Vector2d { X = newVect.X - 1, Y = newVect.Y });
              break;

          }
        }
        else
        {
          path.Pop();
        }
      }
    }
  }

  public class CmdOptions : Options
  {
    /// <summary>
    /// Put your name here.
    /// </summary>
    public string name = "Lukáš Polák";

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static new CmdOptions options = (CmdOptions)(Options.options = new CmdOptions());

    public override void StringStatistics (long[] result)
    {
      if (result == null || result.Length < 4)
        return;

      Util.StringStat(commands, result);
    }

    static CmdOptions ()
    {
      project = "svg098";
      TextPersistence.Register(new CmdOptions(), 0);

      RegisterMsgModes("debug");
    }

    public CmdOptions ()
    {
      // default values of structured members.
      baseDir = @"./";
    }

    public static void Touch ()
    {
      if (options == null)
        Util.Log("CmdOptions not initialized!");
    }

    //--- project-specific options ---

    /// <summary>
    /// Specifies SVG pen size.
    /// </summary>
    public int penSize = 20;

    /// <summary>
    /// Output directory with trailing dir separator.
    /// </summary>
    public string outDir = @"./";

    /// <summary>
    /// Number which decides how tolerant should the generator be whilst generating the maze based on some difficulty.
    /// </summary>
    public double tolerance = 0.05;

    /// <summary>
    /// Number of maze columns (horizontal size in cells).
    /// </summary>
    public int columns = 12;

    /// <summary>
    /// Number of maze rows (vertical size in cells).
    /// </summary>
    public int rows = 8;

    /// <summary>
    /// End X coordinate of the maze.
    /// </summary>
    public int endX = 0;

    /// <summary>
    /// End Y coordinate of the maze.
    /// </summary>
    public int endY = 0;

    /// <summary>
    /// Start X coordinate of the maze.
    /// </summary>
    public int startX = 8;
    
    /// <summary>
    /// Start Y coordinate of the maze.
    /// </summary>
    public int startY = 5;

    /// <summary>
    /// Difficulty coefficient (optional).
    /// </summary>
    public double difficulty = 0.5;

    /// <summary>
    /// Maze width in SVG units (for SVG header).
    /// </summary>
    public double width = 600.0;

    /// <summary>
    /// Maze height in SVG units (for SVG header).
    /// </summary>
    public double height = 400.0;

    /// <summary>
    /// RandomJames generator seed, 0 for randomize.
    /// </summary>
    public long seed = 0L;

    /// <summary>
    /// Generate HTML5 file? (else - direct SVG format)
    /// </summary>
    public bool html = false;

    /// <summary>
    /// Parse additional keys.
    /// </summary>
    /// <param name="key">Key string (non-empty, trimmed).</param>
    /// <param name="value">Value string (non-null, trimmed).</param>
    /// <returns>True if recognized.</returns>
    public override bool AdditionalKey (string key, string value, string line)
    {
      if (base.AdditionalKey(key, value, line))
        return true;

      int newInt = 0;
      long newLong;
      double newDouble = 0.0;

      switch (key)
      {
        case "outDir":
          outDir = value;
          break;

        case "name":
          name = value;
          break;

        case "columns":
          if (int.TryParse(value, out newInt) &&
              newInt > 0)
            columns = newInt;
          break;

        case "rows":
          if (int.TryParse(value, out newInt) &&
              newInt > 0)
            rows = newInt;
          break;

        case "penSize":
          if (int.TryParse(value, out newInt) &&
              newInt > 0)
            penSize = newInt;
          break;

        case "difficulty":
          if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble) &&
              newDouble >= 0.0 && newDouble <= 1.0)
            difficulty = newDouble;
          break;

        case "tolerance":
          if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble) &&
              newDouble >= 0.0 && newDouble <= 1.0)
            tolerance = newDouble;
          break;

        case "width":
          if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble) &&
              newDouble > 0)
            width = newDouble;
          break;

        case "height":
          if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble) &&
              newDouble > 0)
            height = newDouble;
          break;

        case "seed":
          if (long.TryParse(value, out newLong) &&
              newLong >= 0L)
            seed = newLong;
          break;
        
        case "start":
          //parse start position in format [x,y]
          //remove specific characters from string
          value = value.Replace("[", "");
          value = value.Replace("]", "");
          value = value.Replace(" ", "");

          string[] start = value.Split(',');
          
          if (start.Length == 2)
          {
            if (int.TryParse(start[0], out newInt) &&
                newInt >= 0 && newInt < columns)
              startX = newInt;
            if (int.TryParse(start[1], out newInt) &&
                newInt >= 0 && newInt < rows)
              startY = newInt;
          }
          break;
        
        case "end":
          //parse end position in format [x,y]
          //remove specific characters from string
          value = value.Replace("[", "");
          value = value.Replace("]", "");
          value = value.Replace(" ", "");

          string[] end = value.Split(',');
          if (end.Length == 2)
          {
            if (int.TryParse(end[0], out newInt) &&
                newInt >= 0 && newInt < columns)
              endX = newInt;
            if (int.TryParse(end[1], out newInt) &&
                newInt >= 0 && newInt < rows)
              endY = newInt;
          }
          break;

        case "html":
          html = Util.positive(value);
          break;

        default:
          return false;
      }

      return true;
    }

    /// <summary>
    /// How to handle the "key=" config line?
    /// </summary>
    /// <returns>True if config line was handled.</returns>
    public override bool HandleEmptyValue (string key)
    {
      switch (key)
      {
        case "seed":
          seed = 0L;
          return true;
      }

      return false;
    }

    /// <summary>
    /// How to handle the non-key-value config line?
    /// </summary>
    /// <param name="line">The nonempty config line.</param>
    /// <returns>True if config line was handled.</returns>
    public override bool HandleCommand (string line)
    {
      switch (line)
      {
        case "generate":
          Program.Generate();
          return true;
      }

      return false;
    }
  }

  
  class Program
  {
    /// <summary>
    /// The 'generate' command was executed at least once..
    /// </summary>
    static bool wasGenerated = false;

    static void Main (string[] args)
    {
      CmdOptions.Touch();

      if (args.Length < 1)
        Console.WriteLine( "Warning: no command-line options, using default values!" );
      else
        for (int i = 0; i < args.Length; i++)
          if (!string.IsNullOrEmpty(args[i]))
          {
            string opt = args[i];
            if (!CmdOptions.options.ParseOption(args, ref i))
              Console.WriteLine($"Warning: invalid option '{opt}'!");
          }

      if (!wasGenerated)
        Generate();
    }

    /// <summary>
    /// Writes one polyline in SVG format to the given output stream.
    /// </summary>
    /// <param name="wri">Opened output stream (must be left open).</param>
    /// <param name="workList">List of vertices.</param>
    /// <param name="x0">Origin - x-coord (will be subtracted from all x-coords).</param>
    /// <param name="y0">Origin - y-coord (will be subtracted from all y-coords)</param>
    /// <param name="color">Line color (default = black).</param>
    static void drawCurve (StreamWriter wri, List<Vector2> workList, double x0, double y0, string color = "#000")
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat(CultureInfo.InvariantCulture, "M{0:f2},{1:f2}",
                      workList[ 0 ].X - x0, workList[ 0 ].Y - y0);
      for (int i = 1; i < workList.Count; i++)
        sb.AppendFormat(CultureInfo.InvariantCulture, "L{0:f2},{1:f2}",
                        workList[ i ].X - x0, workList[ i ].Y - y0);

      wri.WriteLine("<path d=\"{0}\" stroke=\"{1}\" fill=\"none\"/>", sb.ToString(), color);
    }

    public static void drawRectangle(StreamWriter wri, double x, double y, double size, string color="#000") {
      wri.WriteLine(string.Format(CultureInfo.InvariantCulture, "<rect x=\"{0:f2}\" y=\"{1:f2}\" width=\"{2:f2}\" height=\"{2:f2}\" stroke=\"none\" fill=\"{3}\"/>",
                    x, y, size, color));
    }

    static public void DrawLine(StreamWriter wri, int originX, int originY, float size, DrawWall dir, int R = 0, int G = 0, int B = 0)
    {
      List<Vector2> workList = new List<Vector2>();

      switch (dir)
      {
        case DrawWall.LEFT:
          workList.Add(new Vector2 { X = 0, Y = 0 });
          workList.Add(new Vector2 { X = 0, Y = size });
          break;
        case DrawWall.RIGHT:
          workList.Add(new Vector2 { X = size, Y = 0 });
          workList.Add(new Vector2 { X = size, Y = size });
          break;
        case DrawWall.TOP:
          workList.Add(new Vector2 { X = 0, Y = 0 });
          workList.Add(new Vector2 { X = size, Y = 0 });
          break;
        case DrawWall.BOTTOM:
          workList.Add(new Vector2 { X = 0, Y = size });
          workList.Add(new Vector2 { X = size, Y = size });
          break;
      }

      drawCurve(wri, workList, originX, originY, string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B));
    }

    static public void Generate ()
    {
      wasGenerated = true;

      string fileName = CmdOptions.options.outputFileName;
      if (string.IsNullOrEmpty(fileName))
        fileName = CmdOptions.options.html ? "out.html" : "out.svg";
      string outFn = Path.Combine(CmdOptions.options.outDir, fileName);

      // SVG output.
      using (StreamWriter wri = new StreamWriter(outFn))
      {
        if (CmdOptions.options.html)
        {
          wri.WriteLine("<!DOCTYPE html>");
          wri.WriteLine("<meta charset=\"utf-8\">");
          wri.WriteLine($"<title>SVG Maze ({CmdOptions.options.name})</title>");
          wri.WriteLine(string.Format(CultureInfo.InvariantCulture, "<svg width=\"{0:f0}\" height=\"{1:f0}\">",
                                      CmdOptions.options.width, CmdOptions.options.height));
        }
        else
          wri.WriteLine(string.Format(CultureInfo.InvariantCulture, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0:f0}\" height=\"{1:f0}\">",
                                      CmdOptions.options.width, CmdOptions.options.height));


        RandomJames rnd = new RandomJames();
        if (CmdOptions.options.seed > 0L)
          rnd.Reset(CmdOptions.options.seed);
        else
          rnd.Randomize();
        
        int mazeWidth = CmdOptions.options.columns;
        int mazeHeight = CmdOptions.options.rows;

        //at first, generate 50 mazes to get the minimum depth and the maximum depth needed to solve (for the difficulty option)
        //the depth is calculated by BFS
        int minDepth = mazeHeight*mazeWidth;
        int maxDepth = -1;
        for(int i = 0; i < 50; i++) {
          Maze maze = new Maze(mazeHeight, mazeWidth, rnd);
          maze.Generate();
          int? depth = maze.FindShortestPath(CmdOptions.options.endX, CmdOptions.options.endY, CmdOptions.options.startX, CmdOptions.options.startY);
          if(depth != null) {
            if(depth < minDepth) {
              minDepth = (int)depth;
            }
            if(depth > maxDepth) {
              maxDepth = (int)depth;
            }
          }
        }

        int normalizedDifficulty = (int)Math.Round(((maxDepth - minDepth) * CmdOptions.options.difficulty) + minDepth);

        //then generate the maze with the desired normalized difficulty
        //if this while would run for too long, then the user should lower the tolerance
        do {
          Maze maze = new Maze(mazeHeight, mazeWidth, rnd);
          maze.Generate();
          int? depth = maze.FindShortestPath(CmdOptions.options.endX, CmdOptions.options.endY, CmdOptions.options.startX, CmdOptions.options.startY);
          if(depth != null) {
            if(Math.Abs(normalizedDifficulty - (int)depth) < normalizedDifficulty * 0.05) // with tolerance
            {
              maze.WriteToSVG(wri);
              break;
            }
          }
        } while(true);

        //and then color the starting and ending points
        int penSize = CmdOptions.options.penSize;

        drawRectangle(wri, CmdOptions.options.endX*penSize + 1, CmdOptions.options.endY*penSize + 1, penSize - 2, "#ff0000");
        drawRectangle(wri, CmdOptions.options.startX*penSize + 1, CmdOptions.options.startY*penSize + 1, penSize - 2, "#00ff00");
        wri.WriteLine("</svg>");
      }
    }
  }
}
