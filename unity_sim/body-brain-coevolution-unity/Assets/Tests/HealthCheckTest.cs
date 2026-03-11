using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class HealthCheckTest
{
    [Test]
    public void SanityCheck_MathWorks()
    {
        Assert.AreEqual(2 + 2, 4);
    }
}
