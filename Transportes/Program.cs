using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Transportes
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                // TODO: Reemplaza la ruta del archivo por la ubicación de tu archivo
                string ruta_archivo = @"C:\Users\Steven\Desktop\msf_modelo_transporte\Transportes\Parametros.xml";
                XDocument documento = XDocument.Load(ruta_archivo);
                
                DataSet ds = new DataSet();
                ds.ReadXml(documento.CreateReader());

                DataTable tDemanda = ds.Tables["demanda"].Clone();
                tDemanda.Columns["valor"].DataType = typeof(int);
                tDemanda.Columns["distribuidor"].DataType = typeof(string);
                foreach (DataRow row in ds.Tables["demanda"].Rows)
                    tDemanda.ImportRow(row);

                DataTable tDisponibilidad = ds.Tables["disponibilidad"].Clone();
                tDisponibilidad.Columns["valor"].DataType = typeof(int);
                tDisponibilidad.Columns["fabrica"].DataType = typeof(string);
                foreach (DataRow row in ds.Tables["disponibilidad"].Rows)
                    tDisponibilidad.ImportRow(row);

                DataTable tCosto = ds.Tables["costo"].Clone();
                tCosto.Columns["valor"].DataType = typeof(int);
                tCosto.Columns["fabrica"].DataType = typeof(string);
                tCosto.Columns["distribuidor"].DataType = typeof(string);
                foreach (DataRow row in ds.Tables["costo"].Rows)
                    tCosto.ImportRow(row);

                SolverContext context = SolverContext.GetContext();
                context.ClearModel();
                Model model = context.CreateModel();

                Set fabricas = new Set(Domain.Any, "fabricas");
                Set distribuidores = new Set(Domain.Any, "distribuidores");

                Parameter demanda = new Parameter(Domain.Integer, "demanda", distribuidores);
                demanda.SetBinding(tDemanda.AsEnumerable(), "valor", "distribuidor");

                Parameter costos = new Parameter(Domain.Integer, "costos", fabricas, distribuidores);
                costos.SetBinding(tCosto.AsEnumerable(), "valor", "fabrica", "distribuidor");

                Parameter disponibilidad = new Parameter(Domain.Integer, "disponibilidad", fabricas);
                disponibilidad.SetBinding(tDisponibilidad.AsEnumerable(), "valor", "fabrica");

                model.AddParameters(demanda, costos, disponibilidad);

                Decision x = new Decision(Domain.RealNonnegative, "x", fabricas, distribuidores);
                model.AddDecision(x);

                model.AddConstraint("Disponibilidad", Model.ForEach(fabricas, f => Model.Sum(Model.ForEach(distribuidores, d => x[f, d])) <= disponibilidad[f]));
                model.AddConstraint("Demanda", Model.ForEach(distribuidores, d => Model.Sum(Model.ForEach(fabricas, f => x[f, d])) >= demanda[d]));

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
    }
}
