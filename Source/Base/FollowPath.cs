using System.Text.Json;
using Microsoft.Xna.Framework;

public class FollowPath
{
    private float _moveSpeed;
    private Vector3 _direction;
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
        if (Vector3.DistanceSquared(position, _destination) < 1f)
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
    }
}