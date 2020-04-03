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
  private void RunScript(List<Point3d> P, Mesh M, double step, bool reset, bool go, ref object Pos, ref object status, ref object Trails)
  {
    
    if (reset || GD == null || P.Count != GD.parts.Count) GD = new GradientDescent(M, P);

    if (go)
    {

      // update live variables
      GD.MaxSpeed = step;


      // update System
      GD.Update();


      // expiring solution forces the component to update
      Component.ExpireSolution(true);
    }

    // extracts positions and trails
    pts = new Point3d[P.Count];
    trs = new Polyline[P.Count];
    pStat = new bool[P.Count];
    for(int i = 0; i < GD.parts.Count; i++)
    {
      pts[i] = GD.parts[i].pos;
      if(GD.parts[i].trail.IsValid)trs[i] = GD.parts[i].trail;
      else trs[i] = null;
      pStat[i] = GD.parts[i].alive;
    }

    Pos = pts;
    status = pStat;
    Trails = trs;
  }

  // <Custom additional code> 
  
  // persistent variables
  public GradientDescent GD;

  Point3d[] pts;
  Polyline[] trs;
  bool[] pStat;


  // simulation class
  public class GradientDescent
  {
    public List<Particle> parts;
    public Mesh M;
    public double MaxSpeed;

    public GradientDescent(Mesh M, List<Point3d> P)
    {
      this.M = M;
      parts = new List<Particle>();
      foreach(Point3d p in P) parts.Add(new Particle(this, p));
    }

    public void Update()
    {
      foreach(Particle pa in parts)
        pa.update();
    }

    // update (multithreading)
    public void UpdatePar()
    {
      Parallel.ForEach(parts, pa => {pa.update();});
    }

  }


  // Particle class

  public class Particle // in this case public is unnecessary but in the general case it is
  {

    // fields
    public Point3d pos;
    public Vector3d vel;
    public Polyline trail;
    public bool alive;
    public GradientDescent GraDesc;


    // constructor (public if we use it from RunScript)
    public Particle(GradientDescent GraDesc, Point3d pos){
      this.GraDesc = GraDesc;
      this.pos = pos;
      vel = Vector3d.Zero;
      trail = new Polyline();
      trail.Add(new Point3d(pos));

      alive = true;
    }

    // methods

    public void update()
    {
      calcVel();
      move();
    }

    void calcVel()
    {
      // get closest point to the Mesh
      MeshPoint mP = GraDesc.M.ClosestMeshPoint(pos, 0.0);
      // evaluate Mesh normal at that point
      Vector3d mN = GraDesc.M.NormalAt(mP);
      // find tangent to mesh (perpendicular vector to normal)
      vel = Vector3d.CrossProduct(mN, Vector3d.ZAxis);
      // rotate 90 degrees
      vel.Rotate(Math.PI * 0.5, mN);
      // unitize
      vel.Unitize();
      // set to max speed (step)
      vel *= GraDesc.MaxSpeed;
    }


    void move()
    {
      // calculates new position
      Point3d newPos = pos + vel;
      newPos = GraDesc.M.ClosestPoint(newPos);

      // kills particle if it has reached a pit - update if not
      if (Math.Abs(newPos.Z - pos.Z) < 0.01) alive = false;
      else
      {
        pos = newPos;
        trail.Add(pos);
      }
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
        List<Point3d> P = null;
    if (inputs[0] != null)
    {
      P = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    Mesh M = default(Mesh);
    if (inputs[1] != null)
    {
      M = (Mesh)(inputs[1]);
    }

    double step = default(double);
    if (inputs[2] != null)
    {
      step = (double)(inputs[2]);
    }

    bool reset = default(bool);
    if (inputs[3] != null)
    {
      reset = (bool)(inputs[3]);
    }

    bool go = default(bool);
    if (inputs[4] != null)
    {
      go = (bool)(inputs[4]);
    }



    //3. Declare output parameters
      object Pos = null;
  object status = null;
  object Trails = null;


    //4. Invoke RunScript
    RunScript(P, M, step, reset, go, ref Pos, ref status, ref Trails);
      
    try
    {
      //5. Assign output parameters to component...
            if (Pos != null)
      {
        if (GH_Format.TreatAsCollection(Pos))
        {
          IEnumerable __enum_Pos = (IEnumerable)(Pos);
          DA.SetDataList(1, __enum_Pos);
        }
        else
        {
          if (Pos is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(Pos));
          }
          else
          {
            //assign direct
            DA.SetData(1, Pos);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (status != null)
      {
        if (GH_Format.TreatAsCollection(status))
        {
          IEnumerable __enum_status = (IEnumerable)(status);
          DA.SetDataList(2, __enum_status);
        }
        else
        {
          if (status is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(status));
          }
          else
          {
            //assign direct
            DA.SetData(2, status);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (Trails != null)
      {
        if (GH_Format.TreatAsCollection(Trails))
        {
          IEnumerable __enum_Trails = (IEnumerable)(Trails);
          DA.SetDataList(3, __enum_Trails);
        }
        else
        {
          if (Trails is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(Trails));
          }
          else
          {
            //assign direct
            DA.SetData(3, Trails);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
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