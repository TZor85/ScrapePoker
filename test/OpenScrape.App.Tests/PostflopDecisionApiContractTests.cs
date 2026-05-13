using System.Reflection;

using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Guardrail que previene la reintroducción accidental del overload
/// obsoleto de <c>DetermineAction</c> en <see cref="IPostflopDecisionService"/>.
/// </summary>
[TestFixture]
public class PostflopDecisionApiContractTests
{
    [Test]
    public void IPostflopDecisionService_ExposesSingleDetermineAction()
    {
        var determineActionMethods = typeof(IPostflopDecisionService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "DetermineAction")
            .ToList();

        Assert.That(determineActionMethods, Has.Count.EqualTo(1),
            "IPostflopDecisionService debe exponer exactamente un DetermineAction. " +
            "Si ves esto en rojo, es probable que alguien haya reintroducido el overload obsoleto.");

        var method = determineActionMethods[0];

        var parameters = method.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1),
            "El único DetermineAction debe aceptar exactamente un parámetro.");
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(PostflopDecisionInput)),
            "El parámetro debe ser PostflopDecisionInput.");
        Assert.That(method.ReturnType, Is.EqualTo(typeof(PostflopDecisionResult)),
            "El retorno debe ser PostflopDecisionResult.");
    }

    [Test]
    public void IPostflopDecisionService_HasNoObsoleteMembers()
    {
        var obsoleteMembers = typeof(IPostflopDecisionService)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() is not null)
            .Select(m => m.Name)
            .ToList();

        Assert.That(obsoleteMembers, Is.Empty,
            "La interfaz no debe tener miembros [Obsolete]: " + string.Join(", ", obsoleteMembers));
    }

    [Test]
    public void PostflopDecisionService_HasNoObsoleteDetermineAction()
    {
        var obsoleteDetermineAction = typeof(PostflopDecisionService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "DetermineAction" &&
                        m.GetCustomAttribute<ObsoleteAttribute>() is not null)
            .ToList();

        Assert.That(obsoleteDetermineAction, Is.Empty,
            "PostflopDecisionService no debe tener overloads [Obsolete] de DetermineAction.");
    }
}
