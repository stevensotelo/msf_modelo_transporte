using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transportes
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                /* Crea un dataset con los datos */
                /*
                DataSet ds = new DataSet("parametros");
                ds.Tables.Add(getDemanda());
                ds.Tables[0].TableName = "demanda";
                ds.Tables.Add(getCostos());
                ds.Tables[1].TableName = "costo";
                ds.Tables.Add(getDisponibilidad());
                ds.Tables[2].TableName = "disponibilidad";
                ds.WriteXml("data.ds");*/

                //https://nathanbrixius.wordpress.com/2009/04/24/modeling-a-production-planning-problem-using-solver-foundation/
                SolverContext context = SolverContext.GetContext();
                context.ClearModel();
                Model model = context.CreateModel();

                Set fabricas = new Set(Domain.Any, "fabricas");
                Set distribuidores = new Set(Domain.Any, "distribuidores");

                Parameter demanda = new Parameter(Domain.Integer, "demanda", distribuidores);
                demanda.SetBinding(getDemanda().AsEnumerable(), "valor", "distribuidor");

                Parameter costos = new Parameter(Domain.Integer, "costos", fabricas, distribuidores);
                costos.SetBinding(getCostos().AsEnumerable(), "valor", "fabrica", "distribuidor");

                Parameter disponibilidad = new Parameter(Domain.Integer, "disponibilidad", fabricas);
                disponibilidad.SetBinding(getDisponibilidad().AsEnumerable(), "valor", "fabrica");

                model.AddParameters(demanda, costos, disponibilidad);

                Decision x = new Decision(Domain.RealNonnegative, "x", fabricas, distribuidores);
                model.AddDecision(x);

                model.AddConstraint("Disponibilidad", Model.ForEach(fabricas, f => Model.Sum(Model.ForEach(distribuidores, d => x[f, d])) <= disponibilidad[f]));
                model.AddConstraint("Demanda", Model.ForEach(distribuidores, d => Model.Sum(Model.ForEach(fabricas, f =>  x[f, d])) >= demanda[d]));
                
                model.AddGoal("Meta", GoalKind.Minimize, Model.Sum(Model.ForEach(fabricas, f => Model.ForEach(distribuidores, d => costos[f, d] * x[f, d]))));

                Solution solution = context.Solve(new SimplexDirective());
                Report report = solution.GetReport();
                Console.WriteLine(report);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        public static DataTable getCostos()
        {
            DataTable r = new DataTable();
            r.Columns.Add("fabrica", typeof(string));
            r.Columns.Add("distribuidor", typeof(string));
            r.Columns.Add("valor", typeof(int));
            for (int i = 0; i < fabricas.Count(); i++)
            {
                for (int j = 0; j < distribuidores.Count(); j++)
                {
                    DataRow fila = r.NewRow();
                    fila[0] = fabricas[i];
                    fila[1] = distribuidores[j];
                    fila[2] = costos[i][j];
                    r.Rows.Add(fila);
                }

            }
            return r;
        }

        public static DataTable getDisponibilidad()
        {
            DataTable r = new DataTable();
            r.Columns.Add("fabrica", typeof(string));
            r.Columns.Add("valor", typeof(int));
            for (int i = 0; i < disponibilidad.Length; i++)
            {
                DataRow fila = r.NewRow();
                fila[0] = fabricas[i];
                fila[1] = disponibilidad[i];
                r.Rows.Add(fila);
            }
            return r;
        }

        public static DataTable getDemanda()
        {
            DataTable r = new DataTable();
            r.Columns.Add("distribuidor", typeof(string));
            r.Columns.Add("valor", typeof(int));
            for (int i = 0; i < demanda.Length; i++)
            {
                DataRow fila = r.NewRow();
                fila[0] = distribuidores[i];
                fila[1] = demanda[i];
                r.Rows.Add(fila);
            }
            return r;
        }

        public static string[] fabricas = new string[] { "Fabrica 1", "Fabrica 2", "Fabrica 3", "Fabrica 4" };        
        public static string[] distribuidores = new string[] { "Distribuidor 1", "Distribuidor 2", "Distribuidor 3", "Distribuidor 4", "Distribuidor 5" };

        public static int[][] costos = new int[][] { new int[] { 20, 19, 14, 21, 16 }, new int[] { 15, 20, 13, 19, 16 }, new int[] { 18, 15, 18, 20, int.MaxValue }, new int[] { 0, 0, 0, 0, 0 } };
        public static int[] demanda = new int[] { 30, 40, 50, 40, 60 };
        public static int[] disponibilidad = new int[] { 40, 60, 70, 50 };
        
    }
}
