using System;
using System.Collections.Generic;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fracture.Engine.Tests.Graphics
{
    [Trait("Category", "Graphics")]
    public class GraphicsElementLayerTests
    {
        #region Constant fields
        private const int TestElementTypeId = 32;
        #endregion

        #region Fields
        private readonly GraphicsElementLayer layer;
        #endregion

        public GraphicsElementLayerTests()
            => layer = new GraphicsElementLayer("graphics-layer", 0);

        [Fact]
        public void GraphicsElementLayer_Ctor_Test()
        {
            Assert.Throws<ArgumentNullException>(() => new GraphicsElementLayer(null, 0));
            Assert.Throws<ArgumentNullException>(() => new GraphicsElementLayer("", 0));

            Assert.Null((Record.Exception(() => new GraphicsElementLayer("test-layer", 0))));
        }

        private sealed class ClampTestData : TheoryData<Aabb, Aabb, bool>
        {
            public ClampTestData()
            {
                // This aabb should be clamped from top.
                Add(new Aabb(new Vector2(10.0f, -10.0f), new Vector2(5.0f)),
                    new Aabb(new Vector2(10.0f, 5.0f), new Vector2(5.0f)),
                    true);

                // This aabb should be clamped from left.
                Add(new Aabb(new Vector2(-10.0f, 10.0f), new Vector2(5.0f)),
                    new Aabb(new Vector2(5.0f, 10.0f), new Vector2(5.0f)),
                    true);

                // This aabb should be clamped from both sides.
                Add(new Aabb(new Vector2(-10.0f), new Vector2(5.0f)),
                    new Aabb(new Vector2(5.0f), new Vector2(5.0f)),
                    true);

                // This aabb should not be clamped from any sides.
                Add(new Aabb(new Vector2(10.0f), new Vector2(5.0f)),
                    new Aabb(new Vector2(10.0f), new Vector2(5.0f)),
                    false);
            }
        }

        [Theory]
        [ClassData(typeof(ClampTestData))]
        public void Add_Clamps_When_Element_Overlaps_From_Top_Or_Left(Aabb aabb, Aabb expectedAabb, bool expectedClamped)
        {
            // Act.
            layer.Add(0, TestElementTypeId, ref aabb, out var clamped);

            // Assert.
            Assert.Equal(expectedAabb, aabb);
            Assert.Equal(expectedClamped, clamped);
        }

        [Fact]
        public void Add_Grows_Grid_When_Element_Overlaps_From_Bottom_Or_Right()
        {
            // Arrange.
            var rowsBeforeGrowth    = layer.Rows;
            var columnsBeforeGrowth = layer.Columns;

            // Act.
            var aabb = new Aabb(new Vector2(250.0f), new Vector2(1.0f));

            layer.Add(0, TestElementTypeId, ref aabb, out _);

            // Assert.
            Assert.True(layer.Rows > rowsBeforeGrowth);
            Assert.True(layer.Columns > columnsBeforeGrowth);
        }

        [Fact]
        public void Query_Returns_All_Elements_In_Area()
        {
            // Arrange.
            const int Id1 = 0;
            const int Id2 = 1;

            var aabb1 = new Aabb(new Vector2(40.0f), new Vector2(2.5f));
            var aabb2 = new Aabb(new Vector2(25.0f), new Vector2(2.5f));

            layer.Add(0, TestElementTypeId, ref aabb1, out _);
            layer.Add(1, TestElementTypeId, ref aabb2, out _);

            var resultsContainingNone = new HashSet<GraphicsElement>();
            var resultsContainingId2  = new HashSet<GraphicsElement>();
            var resultsContainingBoth = new HashSet<GraphicsElement>();

            // Act.
            layer.QueryArea(new Aabb(new Vector2(5.0f), new Vector2(2.5f)), resultsContainingNone);
            layer.QueryArea(new Aabb(new Vector2(20.0f), new Vector2(10.0f)), resultsContainingId2);
            layer.QueryArea(new Aabb(new Vector2(40.0f), new Vector2(40.0f)), resultsContainingBoth);

            // Assert.
            Assert.Empty(resultsContainingNone);
            Assert.True(resultsContainingId2.Contains(new GraphicsElement(Id2, TestElementTypeId)));

            Assert.True(resultsContainingBoth.Contains(new GraphicsElement(Id1, TestElementTypeId)));
            Assert.True(resultsContainingBoth.Contains(new GraphicsElement(Id2, TestElementTypeId)));
        }

        [Fact]
        public void Query_Works_With_Overlapping_Aabb()
        {
            // Act.
            var exception = Record.Exception(() =>
                                                 layer.QueryArea(new Aabb(new Vector2(40.0f),
                                                                          new Vector2(1000.0f)),
                                                                 new HashSet<GraphicsElement>()));

            // Assert.
            Assert.Null(exception);
        }
    }
}