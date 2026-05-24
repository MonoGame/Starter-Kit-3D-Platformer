using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Handles following a predefined path using spline points.
/// </summary>
public class FollowPath
{
    private float _moveSpeed;
    private Vector3 _direction;
    private Vector3 _previousPosition;
    private Vector3 _destination;
    private Vector3[] _pathPoints;
    private int _currentPathIndex = 1;
    private int _moveDirection = 1; // 1 for forward, -1 for backward

    /// <summary>
    /// Loads path data from a JSON element.
    /// The json should contain a "movespeed" property and a "splines" array with points.
    /// </summary>
    /// <param name="data">The JSON element containing path data.</param>
    /// <returns>The starting position of the path.</returns>
    public Vector3 LoadFromContent(SceneNodeContent data)
    {
        if (!data.HasMoveSpeed)
            throw new InvalidOperationException($"Scene node '{data.Name}' is missing 'movespeed'.");

        _moveSpeed = data.MoveSpeed;

        if (data.Splines != null)
        {
            foreach (var spline in data.Splines)
            {
                _pathPoints = spline.Points.ToArray();
            }
        }

        if (_pathPoints == null || _pathPoints.Length < 2)
            throw new InvalidOperationException($"Scene node '{data.Name}' requires a spline with at least two points.");

        _direction = Vector3.Normalize(_pathPoints[_currentPathIndex] - _pathPoints[0]);
        _destination = _pathPoints[_currentPathIndex];
        _moveDirection = 1; // Start moving towards the first destination

        return _pathPoints[0];
    }

    /// <summary>
    /// Gets the current velocity based on the move speed and direction.
    /// The velocity is a vector pointing towards the next path point.
    /// The move speed is defined in the JSON data.
    /// </summary>
    /// <returns>The current velocity vector.</returns>
    public Vector3 GetVelocity()
    {
        return _direction * _moveSpeed;
    }

    /// <summary>
    /// Updates the path following logic based on the current position.
    /// This checks if the object has reached the current destination and updates to the next point if necessary.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    /// <param name="position">The current position of the object following the path.</param>
    public void Update(GameTime gameTime, Vector3 position)
    {
        if (_pathPoints == null || _pathPoints.Length == 0)
            return;

        // Check if we need to move to the next point
        float lastDistance = Vector3.Distance(_previousPosition, _destination);
        float currentDistance = Vector3.Distance(position, _destination);

        // Check if we've reached the destination (either close enough or overshot it)
        bool reachedDestination = currentDistance < 1f ||
            (lastDistance > 0 && currentDistance > lastDistance &&
             Vector3.Dot(_direction, Vector3.Normalize(position - _previousPosition)) > 0.8f);

        if (reachedDestination)
        {
            _currentPathIndex += _moveDirection;
            if (_currentPathIndex > _pathPoints.Length - 1)
            {
                _currentPathIndex = _currentPathIndex - 2; // go back to the first point
                _moveDirection = -1;
                _destination = _pathPoints[_currentPathIndex];
            }
            else if (_currentPathIndex < 0)
            {
                _currentPathIndex = 1; // Loop back to the end
                _moveDirection = 1;
                _destination = _pathPoints[_currentPathIndex];
            }
            else
            {
                _destination = _pathPoints[_currentPathIndex];
            }
            _direction = Vector3.Normalize(_destination - position);
        }
        _previousPosition = position;
    }

#if DEVMODE
    VertexBuffer _vertexBuffer;

    /// <summary>
    /// Draws the debug path using line segments.
    /// We don't want to include this in the final build.
    /// So it is behind the DEVMODE conditional compilation symbol.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    public void DrawDebugPath(GraphicsDevice graphicsDevice)
    {
        if (_pathPoints == null || _pathPoints.Length == 0)
            return;

        // Create a vertex buffer if it doesn't exist
        if (_vertexBuffer == null)
        {
            _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), _pathPoints.Length, BufferUsage.WriteOnly);
        }

        // Update the vertex buffer with the current path points
        var vertices = new VertexPositionColor[_pathPoints.Length];
        for (int i = 0; i < _pathPoints.Length; i++)
        {
            vertices[i] = new VertexPositionColor(_pathPoints[i], Color.Red);
        }
        _vertexBuffer.SetData(vertices);

        graphicsDevice.SetVertexBuffer(_vertexBuffer);

        graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, _pathPoints.Length - 1);
    }
#endif
}