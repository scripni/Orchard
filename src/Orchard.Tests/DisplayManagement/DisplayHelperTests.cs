﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ClaySharp;
using ClaySharp.Implementation;
using Moq;
using NUnit.Framework;
using Orchard.DisplayManagement;
using Orchard.DisplayManagement.Shapes;

namespace Orchard.Tests.DisplayManagement {
    [TestFixture]
    public class DisplayHelperTests {
        [Test]
        public void DisplayingShapeWithArgumentsStatically() {
            var viewContext = new ViewContext();

            var displayManager = new Mock<IDisplayManager>();
            var shapeFactory = new Mock<IShapeBuilder>();

            var displayHelperFactory = new DisplayHelperFactory(displayManager.Object, shapeFactory.Object);
            var displayHelper = displayHelperFactory.CreateDisplayHelper(viewContext, null);

            displayHelper.Invoke("Pager", ArgsUtility.Positional(1, 2, 3, 4));

            shapeFactory.Verify(sf=>sf.Build("Pager", It.IsAny<INamedEnumerable<object>>()));
            //displayManager.Verify(dm => dm.Execute(It.IsAny<Shape>(), viewContext, null));
        }
        [Test]
        public void DisplayingShapeWithArgumentsDynamically() {
            var viewContext = new ViewContext();

            var displayManager = new Mock<IDisplayManager>();
            var shapeFactory = new Mock<IShapeBuilder>();

            var displayHelperFactory = new DisplayHelperFactory(displayManager.Object, shapeFactory.Object);
            var display = (dynamic)displayHelperFactory.CreateDisplayHelper(viewContext, null);

            display.Pager(1, 2, 3, 4);

            shapeFactory.Verify(sf => sf.Build("Pager", It.IsAny<INamedEnumerable<object>>()));
            //displayManager.Verify(dm => dm.Execute(It.IsAny<Shape>(), viewContext, null));
        }



        [Test]
        public void UsingDisplayAsFunctionAndPassingInTheShape() {
            var viewContext = new ViewContext();

            var displayManager = new Mock<IDisplayManager>();
            var shapeFactory = new Mock<IShapeBuilder>();

            var displayHelperFactory = new DisplayHelperFactory(displayManager.Object, shapeFactory.Object);
            var display = (dynamic)displayHelperFactory.CreateDisplayHelper(viewContext, null);
            var outline = new Shape { Attributes = new ShapeAttributes { Type = "Outline" } };
            display(outline);

            //displayManager.Verify(dm => dm.Execute(outline, viewContext, null));
        }
    }
}
