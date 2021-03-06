using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;


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
  private void RunScript(Mesh mesh, Vector3d direction, Color color, double intensity, ref object result)
  {
    
    if(mesh.Normals.Count != mesh.Vertices.Count)
      mesh.Normals.ComputeNormals();

    if(mesh.VertexColors.Count != mesh.Vertices.Count)
      mesh.VertexColors.CreateMonotoneMesh(Color.White);

    NormalShader(mesh, direction, color, intensity);
    result = mesh;

  }

  // <Custom additional code> 
  
  //
  void NormalShader(Mesh mesh, Vector3d direction, Color color, double intensity)
  {
    if(!direction.Unitize())
      throw new System.ArgumentException("the direction vector cannot be zero length");

    Color[] vc = mesh.VertexColors.ToArray();

    var chunks = System.Collections.Concurrent.Partitioner.Create(0, mesh.Vertices.Count);
    System.Threading.Tasks.Parallel.ForEach(chunks, range =>
      {
      for (int i = range.Item1; i < range.Item2; i++)
      {
        double t = direction * mesh.Normals[i] * 0.5 + 0.5;
        vc[i] = Color.FromArgb((int) (vc[i].R * (1 - (t * intensity)) + color.R * (t * intensity)),
          (int) (vc[i].G * (1 - (t * intensity)) + color.G * (t * intensity)),
          (int) (vc[i].B * (1 - (t * intensity)) + color.B * (t * intensity)));
      }
      });

    mesh.VertexColors.SetColors(vc);
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
        Mesh mesh = default(Mesh);
    if (inputs[0] != null)
    {
      mesh = (Mesh)(inputs[0]);
    }

    Vector3d direction = default(Vector3d);
    if (inputs[1] != null)
    {
      direction = (Vector3d)(inputs[1]);
    }

    Color color = default(Color);
    if (inputs[2] != null)
    {
      color = (Color)(inputs[2]);
    }

    double intensity = default(double);
    if (inputs[3] != null)
    {
      intensity = (double)(inputs[3]);
    }



    //3. Declare output parameters
      object result = null;


    //4. Invoke RunScript
    RunScript(mesh, direction, color, intensity, ref result);
      
    try
    {
      //5. Assign output parameters to component...
            if (result != null)
      {
        if (GH_Format.TreatAsCollection(result))
        {
          IEnumerable __enum_result = (IEnumerable)(result);
          DA.SetDataList(1, __enum_result);
        }
        else
        {
          if (result is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(result));
          }
          else
          {
            //assign direct
            DA.SetData(1, result);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
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