using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class FollowPath
{
    private float _moveSpeed;
    private Vector3 _direction;
    private Vector3 _previousPosition;
    private Vector3 _destination;
    private Vector3[] _pathPoints;
    private int _currentPathIndex = 1;
    private int _moveDirection = 1; // 1 for forward, -1 for backward

    public Vector3 LoadFromJson(JsonElement data)
    {
        _moveSpeed = data.GetProperty("movespeed").GetSingle();

        if (data.TryGetProperty("splines", out var splineValue))
        {
            if (splineValue.ValueKind == JsonValueKind.Array)
            {
                foreach (var spline in splineValue.EnumerateArray())
                {
                    // Process each spline point
                    // This could be used to adjust the platform's path

                    // Handle spline data if necessary
                    // This could be used for more complex movement patterns
                    var type = spline.GetProperty("type").GetString();
                    _pathPoints = spline.GetProperty("points").ReadSplineFromJson();
                }
            }
            ;
            _direction = Vector3.Normalize(_pathPoints[_currentPathIndex] - _pathPoints[0]);
            _destination = _pathPoints[_currentPathIndex];
            _moveDirection = 1; // Start moving towards the first destination
        }
        return _pathPoints[0];
    }

    public Vector3 GetVelocity()
    {
        return _direction * _moveSpeed;
    }

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