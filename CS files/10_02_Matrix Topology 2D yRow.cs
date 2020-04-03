using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



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
  private void RunScript(int x, int y, int ind, bool wrap, ref object P, ref object nInd)
  {
        ptsArray = initPts(x, y);
    P = ptsArray;
    nInd = neighInd(ind, x, y, wrap);
  }

  // <Custom additional code> 
  
  Point3d[] ptsArray;
  //int[][] nInd;

  // in matrix indexes, y are rows and x columns indexes
  Point3d[] initPts(int x, int y)
  {
    Point3d[] ptsArray = new Point3d[x * y];

    for(int i = 0; i < x; i++)
      for(int j = 0; j < y; j++)
        ptsArray[i * y + j] = new Point3d(i, j, 0);
    return ptsArray;
  }

  int[] neighInd(int i, int x, int y, bool wrap)
  {

        /*
 neighbour indexes schematic:

         5   1   4
           \ | /
         2 - i - 0
  Y+       / | \
  |      6   3   7
  o - X+

  */

    int[] nI = new int[8];
    int ix, iy, ixp, iyp, ixm, iym;
    ix = (int) Math.Floor(i / (double) y);
    iy = i % y;

    ixp = ix + 1;
    ixm = ix - 1;
    iyp = iy + 1;
    iym = iy - 1;

    // compute neighbour indexes
    // (in CCW order - 4 principal directions first, then corners)
    nI[0] = (ixp % x) * y + iy;
    nI[1] = ix * y + (iyp % y);
    nI[2] = ((ixm + x) % x) * y + iy;
    nI[3] = ix * y + ((iym + y) % y);
    nI[4] = (ixp % x) * y + (iyp % y);
    nI[5] = ((ixm + x) % x) * y + (iyp % y);
    nI[6] = ((ixm + x) % x) * y + ((iym + y) % y);
    nI[7] = (ixp % x) * y + ((iym + y) % y);

    if(!wrap)
    {
      // boundary check x

      if(ixp == x)
      {
        nI[0] = -1;
        nI[4] = -1;
        nI[7] = -1;
      } else if (ixm < 0)
      {
        nI[2] = -1;
        nI[5] = -1;
        nI[6] = -1;
      }

      // boundary check y
      if (iyp == y)
      {
        nI[5] = -1;
        nI[1] = -1;
        nI[4] = -1;
      } else if(iym < 0)
      {
        nI[6] = -1;
        nI[3] = -1;
        nI[7] = -1;
      }
    }

    return nI;
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
        int x = default(int);
    if (inputs[0] != null)
    {
      x = (int)(inputs[0]);
    }

    int y = default(int);
    if (inputs[1] != null)
    {
      y = (int)(inputs[1]);
    }

    int ind = default(int);
    if (inputs[2] != null)
    {
      ind = (int)(inputs[2]);
    }

    bool wrap = default(bool);
    if (inputs[3] != null)
    {
      wrap = (bool)(inputs[3]);
    }



    //3. Declare output parameters
      object P = null;
  object nInd = null;


    //4. Invoke RunScript
    RunScript(x, y, ind, wrap, ref P, ref nInd);
      
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
      if (nInd != null)
      {
        if (GH_Format.TreatAsCollection(nInd))
        {
          IEnumerable __enum_nInd = (IEnumerable)(nInd);
          DA.SetDataList(2, __enum_nInd);
        }
        else
        {
          if (nInd is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(nInd));
          }
          else
          {
            //assign direct
            DA.SetData(2, nInd);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
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