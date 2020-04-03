using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Threading.Tasks;
using Rhino.Collections;
using System.Linq;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(Point3d P0, int n, double evap, bool wrap, bool reset, bool go, bool MT, ref object P, ref object cPind, ref object nIndexes, ref object S)
  {
        // reset/initialize

    if (reset || diff == null || diff.ptsArray.Length != n * n)
    {

      diff = new Diffusion(n, n, wrap);
      ptsList = new Point3dList(diff.ptsArray); // create a Point3dList for faster closest point calculation
      cP = ptsList.ClosestIndex(P0); // find index of closest point to attractor
      diff.stigVal[cP] = 1;
      c = 0; // reset counter
    }

    if (go)
    {
      diff.evap = evap;
      cP = ptsList.ClosestIndex(P0); // find index of closest point to attractor

      if(MT) diff.UpdateMT(); else diff.Update();
      diff.stigVal[cP] = 1;
      c++;
      Component.ExpireSolution(true);
    }

    P = diff.ptsArray.Select(x => new GH_Point(x));
    S = diff.stigVal.Select(x => new GH_Number(x));
    cPind = cP;
    // nIndexes = diff.GetNeighIndexes();
  }

  // <Custom additional code> 
    Diffusion diff;
  Point3dList ptsList;
  int c, cP;


  // array of diffusion coefficients (in CCW order - 4 principal directions first, then corners)
  public static double[] dC = {0.2, 0.2, 0.2, 0.2, 0.05, 0.05, 0.05, 0.05};


  public class Diffusion
  {
    public Point3d[] ptsArray;
    public int[][] nInd;
    public double[][] nDiff;
    public int x;
    public int y;
    public double evap;
    public bool wrap;
    public double[] stigVal;
    public double[] dC = {0.2, 0.2, 0.2, 0.2, 0.05, 0.05, 0.05, 0.05};


    public Diffusion(int x, int y, bool wrap)
    {
      this.x = x;
      this.y = y;
      this.wrap = wrap;

      initPts();
      initStig();
      BuildNeighInd();
    }

    public void UpdateMT()
    {
      double[] stigMod = new double[stigVal.Length];

      // instead of calculating an outgoing value (writing to neighbours)
      // an incoming contribution from neighbours is calculated (reading from neighbours)
      // hence making parallel computation possible
      Parallel.For(0, stigVal.Length, i =>
        {
        for(int j = 0; j < nInd[i].Length; j++)
          stigMod[i] += stigVal[nInd[i][j]] * nDiff[i][j];
        });

      // still, calculation must be split in 2 parts: calculating modification and value update
      Parallel.For(0, stigVal.Length, i =>
        {
        stigVal[i] += stigMod[i];
        stigVal[i] *= evap;
        //if (stigVal[i] > 1) stigVal[i] = 1;
        stigVal[i] = Math.Min(stigVal[i], 1);
        });


    }

    public void Update()
    {
      double[] stigMod = new double[stigVal.Length];

      // instead of calculating an outgoing value (writing to neighbours)
      // an incoming contribution from neighbours is calculated (reading from neighbours)
      // hence making parallel computation possible
      for(int i = 0; i < stigVal.Length; i++)
      {
        for(int j = 0; j < nInd[i].Length; j++)
          stigMod[i] += stigVal[nInd[i][j]] * nDiff[i][j];
      }

      // still, calculation must be split in 2 parts: calculating modification and value update
      for(int i = 0; i < stigVal.Length; i++)
      {
        stigVal[i] += stigMod[i];
        stigVal[i] *= evap;
        //if (stigVal[i] > 1) stigVal[i] = 1;
        stigVal[i] = Math.Min(stigVal[i], 1);
      }


    }

    public void UpdateOld()
    {
      double[] stigMod = new double[stigVal.Length];

      for(int i = 0; i < stigVal.Length; i++)
        for(int j = 0; j < nInd[i].Length; j++)
          stigMod[nInd[i][j]] += stigVal[i] * dC[j] * 1;

      Parallel.For(0, stigVal.Length, i =>
        //for(int i = 0; i < stigVal.Length; i++)
        {
        stigVal[i] += stigMod[i];
        stigVal[i] *= evap;
        //if (stigVal[i] > 1) stigVal[i] = 1;
        stigVal[i] = Math.Min(stigVal[i], 1);

        });
    }

    void initPts()
    {
      ptsArray = new Point3d[x * y];

      for(int i = 0; i < x; i++)
        for(int j = 0; j < y; j++)
          ptsArray[i * y + j] = new Point3d(i, j, 0);
    }

    void initStig()
    {
      stigVal = new double[x * y];

      for(int i = 0; i < x; i++)
        for(int j = 0; j < y; j++)
          stigVal[i * y + j] = 0.0;
    }

            /*
 neighbour indexes schematic:

         5   1   4
           \ | /
         2 - i - 0
  Y+       / | \
  |      6   3   7
  o - X+

  */

    void BuildNeighInd()
    {
      nInd = new int[x * y][];
      nDiff = new double[x * y][];
      for (int i = 0; i < x * y; i++)
      {
        int ix, iy, ixp, iyp, ixm, iym;
        bool r, u, l, d;
        ix = (int) Math.Floor(i / (double) y);
        iy = i % y;

        ixp = ix + 1;
        ixm = ix - 1;
        iyp = iy + 1;
        iym = iy - 1;

        r = ixp < x;
        u = iyp < y;
        l = ixm >= 0;
        d = iym >= 0;

        List<int> nL = new List<int>();
        List<double> nD = new List<double>();
        // compute neighbour indexes

        if (r || wrap)
        {
          nL.Add((ixp % x) * y + iy);// x+
          nD.Add(dC[0]);
        }
        if (u || wrap)
        {
          nL.Add(ix * y + (iyp % y));// y+
          nD.Add(dC[1]);
        }
        if (l || wrap)
        {
          nL.Add(((ixm + x) % x) * y + iy);// x-
          nD.Add(dC[2]);
        }
        if (d || wrap)
        {
          nL.Add(ix * y + ((iym + y) % y));// y-
          nD.Add(dC[3]);
        }
        if ((r && u) || wrap)
        {
          nL.Add((ixp % x) * y + (iyp % y));// x+ y+
          nD.Add(dC[4]);
        }
        if ((l && u) || wrap)
        {
          nL.Add(((ixm + x) % x) * y + (iyp % y));// x- y+
          nD.Add(dC[5]);
        }
        if ((l && d) || wrap)
        {
          nL.Add(((ixm + x) % x) * y + ((iym + y) % y));// x- y-
          nD.Add(dC[6]);
        }
        if ((r && d) || wrap)
        {
          nL.Add((ixp % x) * y + ((iym + y) % y));// x+ y-
          nD.Add(dC[7]);
        }
        nInd[i] = nL.ToArray();
        nDiff[i] = nD.ToArray();
      }
    }

    public DataTree<GH_Integer> GetNeighIndexes()
    {
      return ToDataTree(nInd);
    }

    DataTree<GH_Integer> ToDataTree(int[][] vals)
    {
      DataTree<GH_Integer> vTree = new DataTree<GH_Integer>();

      for(int i = 0; i < vals.Length; i++)
        vTree.AddRange(vals[i].Select(x => new GH_Integer(x)), new GH_Path(i));
      return vTree;
    }

  }




  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        Point3d P0 = default(Point3d);
    if (inputs[0] != null)
    {
      P0 = (Point3d)(inputs[0]);
    }

    int n = default(int);
    if (inputs[1] != null)
    {
      n = (int)(inputs[1]);
    }

    double evap = default(double);
    if (inputs[2] != null)
    {
      evap = (double)(inputs[2]);
    }

    bool wrap = default(bool);
    if (inputs[3] != null)
    {
      wrap = (bool)(inputs[3]);
    }

    bool reset = default(bool);
    if (inputs[4] != null)
    {
      reset = (bool)(inputs[4]);
    }

    bool go = default(bool);
    if (inputs[5] != null)
    {
      go = (bool)(inputs[5]);
    }

    bool MT = default(bool);
    if (inputs[6] != null)
    {
      MT = (bool)(inputs[6]);
    }



    //3. Declare output parameters
      object P = null;
  object cPind = null;
  object nIndexes = null;
  object S = null;


    //4. Invoke RunScript
    RunScript(P0, n, evap, wrap, reset, go, MT, ref P, ref cPind, ref nIndexes, ref S);
      
    try
    {
      //5. Assign output parameters to component...
            if (P != null)
      {
        if (GH_Format.TreatAsCollection(P))
        {
          IEnumerable __enum_P = (IEnumerable)(P);
          DA.SetDataList(1, __enum_P);
        }
        else
        {
          if (P is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(P));
          }
          else
          {
            //assign direct
            DA.SetData(1, P);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (cPind != null)
      {
        if (GH_Format.TreatAsCollection(cPind))
        {
          IEnumerable __enum_cPind = (IEnumerable)(cPind);
          DA.SetDataList(2, __enum_cPind);
        }
        else
        {
          if (cPind is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(cPind));
          }
          else
          {
            //assign direct
            DA.SetData(2, cPind);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (nIndexes != null)
      {
        if (GH_Format.TreatAsCollection(nIndexes))
        {
          IEnumerable __enum_nIndexes = (IEnumerable)(nIndexes);
          DA.SetDataList(3, __enum_nIndexes);
        }
        else
        {
          if (nIndexes is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(nIndexes));
          }
          else
          {
            //assign direct
            DA.SetData(3, nIndexes);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
      }
      if (S != null)
      {
        if (GH_Format.TreatAsCollection(S))
        {
          IEnumerable __enum_S = (IEnumerable)(S);
          DA.SetDataList(4, __enum_S);
        }
        else
        {
          if (S is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(S));
          }
          else
          {
            //assign direct
            DA.SetData(4, S);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}