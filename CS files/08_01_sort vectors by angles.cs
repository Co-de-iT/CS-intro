using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

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
  private void RunScript(Vector3d V, ref object VSort, ref object vStar)
  {
        VSort = sortVector(V); // sorted vectors
    vStar = vectorStar; // as a check, to verify that vectorStar is not modified
  }

  // <Custom additional code> 
    public static Vector3d[] vectorStar = new Vector3d[]{new Vector3d(-1, 0, 0), new Vector3d(0, -1, 0), new Vector3d(0, 0, -1),
    new Vector3d(1, 0, 0), new Vector3d(0, 1, 0), new Vector3d(0, 0, 1)};

  Vector3d[] sortVector(Vector3d v)
  {

    double[] angles = new double[vectorStar.Length];
    v.Unitize();

    // fill angles array
    angles = vectorStar.Select(x => Vector3d.VectorAngle(v, x)).ToArray(); //using System.Linq;
    //    for(int i = 0; i < vSorted.Length; i++)
    //      angles[i] = Vector3d.VectorAngle(v, vSorted[i]);

    //sorting vectors according to angles
    // taken from: https://stackoverflow.com/questions/29591992/sorting-multiple-arrays-c-sharp
    // if you are wondering what the _ in (_,i) is, check Discards:
    // https://docs.microsoft.com/en-us/dotnet/csharp/discards
    Vector3d[] vSort = (Vector3d[]) vectorStar
      .Select((_, i) => new // this creates a new anonymous class with angles and vectors as fields
      {
        ang = angles[i],
        vec = vectorStar[i]
        })
      .OrderBy(x => x.ang) // sort the anonymous class array by angles
      .Select(x => x.vec) // the new class is an Anonymous type - we need to extract the vectors field only
      .ToArray(); // the result is a generic List class, must be converted to array

    return vSort;
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
        Vector3d V = default(Vector3d);
    if (inputs[0] != null)
    {
      V = (Vector3d)(inputs[0]);
    }



    //3. Declare output parameters
      object VSort = null;
  object vStar = null;


    //4. Invoke RunScript
    RunScript(V, ref VSort, ref vStar);
      
    try
    {
      //5. Assign output parameters to component...
            if (VSort != null)
      {
        if (GH_Format.TreatAsCollection(VSort))
        {
          IEnumerable __enum_VSort = (IEnumerable)(VSort);
          DA.SetDataList(1, __enum_VSort);
        }
        else
        {
          if (VSort is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(VSort));
          }
          else
          {
            //assign direct
            DA.SetData(1, VSort);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (vStar != null)
      {
        if (GH_Format.TreatAsCollection(vStar))
        {
          IEnumerable __enum_vStar = (IEnumerable)(vStar);
          DA.SetDataList(2, __enum_vStar);
        }
        else
        {
          if (vStar is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(vStar));
          }
          else
          {
            //assign direct
            DA.SetData(2, vStar);
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