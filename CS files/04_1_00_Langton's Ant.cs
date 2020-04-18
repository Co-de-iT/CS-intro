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
using System.Threading.Tasks;
using System.Drawing;

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
  private void RunScript(bool go, bool reset, int it, int size, ref object D)
  {
                    /*
  Langton's Ant:

   . At a white square, turn 90° right, flip the color of the square, move forward one unit
   . At a black square, turn 90° left, flip the color of the square, move forward one unit

  */
    if (reset || antSim == null || antSim.size != size)
    {
      antSim = new LangAnt(size);
    }

    if (go)
    {
      for(int i = 0; i < it; i++)
        antSim.Update();
      Component.ExpireSolution(true);
    }

    D = antSim.GetData();

  }

  // <Custom additional code> 
  
  LangAnt antSim;

  class LangAnt
  {
    public int[][] data;
    public int[] pos;
    public int[] vel;
    public int size;
    public LangAnt(int size)
    {
      this.size = size;
      data = InitializeData(size);
      pos = new int[]{(int) Math.Floor(size * 0.5),(int) Math.Floor(size * 0.5)};
      vel = new int[]{1,0}; // x,y
    }

    public int[][] InitializeData(int size)
    {
      int[][] data = new int[size][];

      for(int i = 0; i < data.Length; i++)
        data[i] = Enumerable.Repeat(1, size).ToArray();

      return data;

    }

    public void Update()
    {
      int z = vel[0];

      // At a white square, turn 90° right
      if(data[pos[0]][pos[1]] == 1)
      {
        // turn 90 ° right
        vel[0] = vel[1];
        vel[1] = -z;
      }
      else
        // At a black square, turn 90° left
      {
        // turn 90° left
        vel[0] = -vel[1];
        vel[1] = z;
      }

      // flip square color
      data[pos[0]][pos[1]] = Math.Abs(data[pos[0]][pos[1]] - 1);

      // move forward one unit
      pos[0] += vel[0];
      pos[1] += vel[1];

      // check containment
      pos[0] = (pos[0] + size) % size;
      pos[1] = (pos[1] + size) % size;
    }


    public GH_Integer[] GetData()
    {

      return ToArray(data);
    }


    public DataTree<GH_Integer> ToDataTree(int[][] array)
    {
      DataTree<GH_Integer> dT = new DataTree<GH_Integer>();

      for(int i = 0; i < array.Length; i++)
        dT.AddRange(array[i].Select(x => new GH_Integer(x)), new GH_Path(i));

      return dT;
    }

    public GH_Integer[] ToArray(int[][] array)
    {
      GH_Integer[] rA = new GH_Integer[array.Length * array[0].Length];
      Parallel.For(0, array.Length, i =>
        {
        for(int j = 0; j < array[i].Length; j++)
          rA[i * array[i].Length + j] = new GH_Integer(array[i][j]);

        });
      return rA;
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
        bool go = default(bool);
    if (inputs[0] != null)
    {
      go = (bool)(inputs[0]);
    }

    bool reset = default(bool);
    if (inputs[1] != null)
    {
      reset = (bool)(inputs[1]);
    }

    int it = default(int);
    if (inputs[2] != null)
    {
      it = (int)(inputs[2]);
    }

    int size = default(int);
    if (inputs[3] != null)
    {
      size = (int)(inputs[3]);
    }



    //3. Declare output parameters
      object D = null;


    //4. Invoke RunScript
    RunScript(go, reset, it, size, ref D);
      
    try
    {
      //5. Assign output parameters to component...
            if (D != null)
      {
        if (GH_Format.TreatAsCollection(D))
        {
          IEnumerable __enum_D = (IEnumerable)(D);
          DA.SetDataList(1, __enum_D);
        }
        else
        {
          if (D is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(D));
          }
          else
          {
            //assign direct
            DA.SetData(1, D);
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