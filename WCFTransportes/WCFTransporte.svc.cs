using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace WCFTransportes
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFTransporte" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WCFTransporte.svc or WCFTransporte.svc.cs at the Solution Explorer and start debugging.
    public class WCFTransporte : IWCFTransporte
    {
        public string modeloTransporte(string xmlCostos, string xmlDemanda, string xmlDisponibilidad)
        {
            string r="<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            try
            {
                XDocument xCostos = XDocument.Parse(xmlCostos);
                IEnumerable<XElement> rCostos = from row in xCostos.Descendants("costo") 
                                                select row;

                XDocument xDemanda = XDocument.Parse(xmlCostos);
                IEnumerable<XElement> rDemanda = from row in xDemanda.Descendants("demanda")
                                                select row;

                XDocument xDisponibilidad = XDocument.Parse(xmlCostos);
                IEnumerable<XElement> rDisponibilidad = from row in xDisponibilidad.Descendants("disponibilidad")
                                                select row;

                SolverContext context = SolverContext.GetContext();
                context.ClearModel();
                Model model = context.CreateModel();

                Set fabricas = new Set(Domain.Any, "fabricas");
                Set distribuidores = new Set(Domain.Any, "distribuidores");

                Parameter demanda = new Parameter(Domain.Integer, "demanda", distribuidores);
                demanda.SetBinding(rDemanda, "Demanda", "Distribuidor");

                Parameter costos = new Parameter(Domain.Integer, "costos", fabricas, distribuidores);
                costos.SetBinding(rCostos, "Costo", "Fabrica", "Distribuidor");

                Parameter disponibilidad = new Parameter(Domain.Integer, "disponibilidad", fabricas);
                disponibilidad.SetBinding(rDisponibilidad, "Disponibilidad", "Fabrica");

                model.AddParameters(demanda, costos, disponibilidad);

                Decision x = new Decision(Domain.RealNonnegative, "x", fabricas, distribuidores);
                model.AddDecision(x);

                model.AddConstraint("Disponibilidad", Model.ForEach(fabricas, f => Model.Sum(Model.ForEach(distribuidores, d => x[f, d])) <= disponibilidad[f]));
                model.AddConstraint("Demanda", Model.ForEach(distribuidores, d => Model.Sum(Model.ForEach(fabricas, f => x[f, d])) >= demanda[d]));

                model.AddGoal("Meta", GoalKind.Minimize, Model.Sum(Model.ForEach(fabricas, f => Model.ForEach(distribuidores, d => costos[f, d] * x[f, d]))));

                Solution solution = context.Solve(new SimplexDirective());
                Report report = solution.GetReport();
                r += "<status>ok</status><mensaje>" + report.ToString() + "</mensaje>";
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                r += "<status>error</status><mensaje>" + ex.Message + "</mensaje>";
            }
            return r;
        }
    }
}
