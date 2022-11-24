using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using CircleCanvas;
using MathSupport;

namespace _083animation
{ 
  /// <summary>
  /// Class representing a circle in an Apollonian gasket.
  /// </summary>
  class Circle
  {
    public Complex coordinates;
    public double radius;
    public int frameToRender;
    public bool alreadyRendered;

    public Circle (double x, double y, double r)
    {
      radius = r;
      coordinates = new Complex(x, y);
      alreadyRendered = false;
    }

    /// <summary>
    /// Method returning the inverse of the radius.
    /// </summary>
    /// <returns>Inverse of the radius.</returns>
    public double InverseRadius ()
    {
      return 1 / radius;
    }
  }

  /// <summary>
  /// Class representing Apollonian gaskets.
  /// For more info visit: https://en.wikipedia.org/wiki/Apollonian_gasket
  /// </summary>
  class ApollonianGasket
  {
    List<Circle> initial;
    public List<Circle> generated;

    public ApollonianGasket (double c1, double c2, double c3)
    {
      initial = new List<Circle>();
      generated = new List<Circle>();

      Circle[] circles = ThreeCirclesFromRadii(1 / c1, 1 / c2, 1 / c3);
      foreach (Circle c in circles)
      {
        initial.Add(c);
        generated.Add(c);
      }
    }

    /// <summary>
    /// Method which recursively generates the circles of the current depth and adds them to the generated list.
    /// </summary>
    /// <param name="circles">Starting set of circles.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <param name="maxDepth">Maximal recursion depth.</param>
    void RecursiveGenerate (List<Circle> circles, int depth, int maxDepth)
    {
      if (depth == maxDepth)
      {
        return;
      }

      Circle c1 = circles[0];
      Circle c2 = circles[1];
      Circle c3 = circles[2];
      Circle c4 = circles[3];

      if (depth == 0) //the only time its needed to generate four circles
      {
        if (circles.Count > 4)
        {
          circles.RemoveRange(4, circles.Count - 4);
        }

        Circle cspecial = SecondSolution(c1, c2, c3, c4);
        generated.Add(cspecial);
        RecursiveGenerate(new List<Circle> { cspecial, c2, c3, c4 }, 1, maxDepth);
      }

      Circle cn2 = SecondSolution(c2, c1, c3, c4);
      generated.Add(cn2);
      Circle cn3 = SecondSolution(c3, c1, c2, c4);
      generated.Add(cn3);
      Circle cn4 = SecondSolution(c4, c1, c2, c3);
      generated.Add(cn4);

      RecursiveGenerate(new List<Circle> { cn2, c1, c3, c4 }, depth + 1, maxDepth);
      RecursiveGenerate(new List<Circle> { cn3, c1, c2, c4 }, depth + 1, maxDepth);
      RecursiveGenerate(new List<Circle> { cn4, c1, c2, c3 }, depth + 1, maxDepth);
    }

    /// <summary>
    /// Public API method for generating of the circles.
    /// </summary>
    /// <param name="maxDepth">Maximum depth.</param>
    public void Generate (int maxDepth)
    {
      RecursiveGenerate(initial, 0, maxDepth);
    }

    /// <summary>
    /// Computes the fourth tangent circle.
    /// </summary>
    /// <param name="circle1">First circle.</param>
    /// <param name="circle2">Second circle.</param>
    /// <param name="circle3">Third circle.</param>
    /// <returns>Computed fourth circle.</returns>
    static Circle GetFourthCircle (Circle circle1, Circle circle2, Circle circle3)
    {
      double inverseRadius1 = circle1.InverseRadius();
      double inverseRadius2 = circle2.InverseRadius();
      double inverseRadius3 = circle3.InverseRadius();
      double inverseRadius4 = -2 * Math.Sqrt(inverseRadius1 * inverseRadius2 + inverseRadius2 * inverseRadius3 + inverseRadius1 * inverseRadius3) + inverseRadius1 + inverseRadius2 + inverseRadius3;

      Complex coordinates4 = (-2 * Complex.Sqrt(inverseRadius1 * circle1.coordinates * inverseRadius2 * circle2.coordinates + inverseRadius2 * circle2.coordinates * inverseRadius3 * circle3.coordinates + inverseRadius1 * circle1.coordinates * inverseRadius3 * circle3.coordinates) + inverseRadius1 * circle1.coordinates + inverseRadius2 * circle2.coordinates + inverseRadius3 * circle3.coordinates) / inverseRadius4;

      return new Circle(coordinates4.Real, coordinates4.Imaginary, 1 / inverseRadius4);
    }

    /// <summary>
    /// Method returning three tangent circles from the given radii.
    /// </summary>
    /// <param name="radius2">Radius 2.</param>
    /// <param name="radius3">Radius 3.</param>
    /// <param name="radius4">Radius 4.</param>
    /// <returns>Four circles with given radii.</returns>
    static Circle[] ThreeCirclesFromRadii (double radius2, double radius3, double radius4)
    {
      Circle circle2 = new Circle(0, 0, radius2);
      Circle circle3 = new Circle(radius2 + radius3, 0, radius3);

      double coordinates4X = (radius2 * radius2 + radius2 * radius4 + radius2 * radius3 - radius3 * radius4) / (radius2 + radius3);
      double coordinates4Y = Math.Sqrt((radius2 + radius4) * (radius2 + radius4) - coordinates4X * coordinates4X);
      Circle circle4 = new Circle(coordinates4X, coordinates4Y, radius4);

      Circle circle1 = GetFourthCircle(circle2, circle3, circle4);

      return new Circle[] { circle1, circle2, circle3, circle4 };
    }

    /// <summary>
    /// Given four tangent circles, this method calculates the other tangent circle to the three.
    /// </summary>
    /// <param name="circleF">Fixed circle.</param>
    /// <param name="circle1">First circle.</param>
    /// <param name="circle2">Second circle.</param>
    /// <param name="circle3">Third circle.</param>
    /// <returns>The other tangent circle to 1-3.</returns>
    static Circle SecondSolution (Circle circleF, Circle circle1, Circle circle2, Circle circle3)
    {
      double inverseRadiusF = circleF.InverseRadius();
      double inverseRadius1 = circle1.InverseRadius();
      double inverseRadius2 = circle2.InverseRadius();
      double inverseRadius3 = circle3.InverseRadius();

      double inverseRadiusNew = 2 * (inverseRadius1 + inverseRadius2 + inverseRadius3) - inverseRadiusF;
      Complex coordinatesNew = (2 * (inverseRadius1 * circle1.coordinates + inverseRadius2 * circle2.coordinates + inverseRadius3 * circle3.coordinates) - inverseRadiusF * circleF.coordinates) / inverseRadiusNew;

      return new Circle(coordinatesNew.Real, coordinatesNew.Imaginary, 1 / inverseRadiusNew);
    }
  }

  public class Animation
  {
    static ApollonianGasket ap;
    static double offsetX, offsetY, normalizationConst, size, fps;
    static int paddingX, paddingY;

    /// <summary>
    /// Form data initialization.
    /// </summary>
    /// <param name="name">Your first-name and last-name.</param>
    /// <param name="wid">Initial image width in pixels.</param>
    /// <param name="hei">Initial image height in pixels.</param>
    /// <param name="from">Start time (t0)</param>
    /// <param name="to">End time (for animation length normalization).</param>
    /// <param name="fps">Frames-per-second.</param>
    /// <param name="param">Optional text to initialize the form's text-field.</param>
    /// <param name="tooltip">Optional tooltip = param help.</param>
    public static void InitParams (out string name, out int wid, out int hei, out double from, out double to, out double fps, out string param, out string tooltip)
    {
      name = "Lukáš Polák";

      // Frame size in pixels.
      wid = 1920;
      hei = 1080;

      // Animation.
      from =  0.0;
      to   = 10.0;
      fps  = 25.0;

      Animation.fps = fps;

      // Form params.
      param = "2763";
      tooltip = "<long> .. random seed for the number generator";
    }

    /// <summary>
    /// Global initialization. Called before each animation batch
    /// or single-frame computation.
    /// </summary>
    /// <param name="width">Width of the future canvas in pixels.</param>
    /// <param name="height">Height of the future canvas in pixels.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="fps">Required fps.</param>
    /// <param name="param">Text parameter field from the form.</param>
    public static void InitAnimation (int width, int height, double start, double end, double fps, string param)
    {
      RandomJames rnd = new RandomJames();

      if (long.TryParse(param, NumberStyles.Number, CultureInfo.InvariantCulture, out long seed))
      {
        rnd.Reset(seed);
      }

      ap = new ApollonianGasket(rnd.RandomInteger(3, 20), rnd.RandomInteger(3, 20), rnd.RandomInteger(3, 20));
      ap.Generate(4);

      //it may happen that the generated circles do not exist (values would be inf...), so then just re-generate the values
      while (Double.IsNaN(ap.generated[0].coordinates.Real))
      {
        rnd.Reset(seed++);
        ap = new ApollonianGasket(rnd.RandomInteger(3, 20), rnd.RandomInteger(3, 20), rnd.RandomInteger(3, 20));
        ap.Generate(4);
      }

      //we need to somehow adjust the values from the circles to draw them properly...
      offsetX = ap.generated[0].coordinates.Real;
      offsetY = ap.generated[0].coordinates.Imaginary;
      normalizationConst = 1 / Math.Abs(ap.generated[0].radius);

      size = Math.Min(width, height) / 2;

      if (width > height)
      {
        paddingX = (width - height) / 2;
      }
      else if (height > width)
      {
        paddingY = (height - width) / 2;
      }

      double totalFrames = (end - start) * fps + 1;
      double totalCircles = ap.generated.Count;
      double coefficient = totalFrames / totalCircles;

      // temporarily remove the biggest circle
      Circle c = ap.generated[0];
      _ = ap.generated.Remove(c);

      // shuffle the circles to get a random drawing order
      Extensions.Shuffle(ap.generated);

      ap.generated.Insert(0, c);
      
      for (int i = 0; i < ap.generated.Count; i++)
      {
        ap.generated[i].frameToRender = (int)Math.Floor(coefficient * i) + 3;
      }
    }

    /// <summary>
    /// Draw single animation frame.
    /// </summary>
    /// <param name="c">Canvas to draw to.</param>
    /// <param name="time">Current time in seconds.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void DrawFrame (Canvas c, double time, double start, double end, string param)
    {
      RandomJames rnd = new RandomJames();

      if (long.TryParse(param, NumberStyles.Number, CultureInfo.InvariantCulture, out long seed))
      {
        rnd.Reset(seed);
      }

      c.SetAntiAlias(true);

      // calculates the current frame number
      int frameFromTime = (int)Math.Floor((end - start) * fps) - (int)Math.Floor((end - time) * fps) + 1;

      // Debug.WriteLine("{0}, {1}", frameFromTime, time);

      foreach (Circle circle in ap.generated)
      {
        if (circle.frameToRender > frameFromTime && !circle.alreadyRendered)
        {
          continue;
        }
        circle.alreadyRendered = true;
        c.SetColor(Color.FromArgb(rnd.RandomInteger(0, 255), rnd.RandomInteger(0, 255), rnd.RandomInteger(0, 255)));
        c.FillDisc((float)((circle.coordinates.Real - offsetX) * normalizationConst * size) + (float)size + paddingX, (float)((circle.coordinates.Imaginary - offsetY) * size * normalizationConst) + (float)size + paddingY, (float)Math.Abs(circle.radius * normalizationConst * size));
      }
    }
  }

  static class Extensions
  {
    /// <summary>
    /// Random IList<typeparamref name="T"/> shuffle.
    /// Taken from http://stackoverflow.com/questions/273313/randomize-a-listt
    /// </summary>
    public static void Shuffle<T> (this IList<T> list)
    {
      int n = list.Count;
      RandomJames rnd = new RandomJames();
      while (n > 1)
      {
        n--;
        int k = rnd.RandomInteger(0, n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }
  }
}
