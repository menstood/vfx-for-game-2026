using System;
using System.Collections;
using UnityEngine;

public class CameraTour : MonoBehaviour
{
    [Serializable]
    public class Stop
    {
        public Transform pointOfInterest;
        public Transform cameraPosition;
        public float holdSeconds = 3f;
    }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Stop[] stops;
    [SerializeField] private float moveSeconds = 3f;
    [SerializeField] private bool loop = true;

    private IEnumerator Start()
    {
        if (stops == null || stops.Length == 0)
            yield break;

        SnapTo(stops[0]);
        var index = 0;

        while (true)
        {
            yield return new WaitForSeconds(stops[index].holdSeconds);

            var next = index + 1;
            if (next >= stops.Length)
            {
                if (!loop)
                    yield break;
                next = 0;
            }

            yield return MoveTo(stops[next]);
            index = next;
        }
    }

    private void SnapTo(Stop stop)
    {
        cameraTransform.SetPositionAndRotation(stop.cameraPosition.position, LookRotation(stop));
    }

    private IEnumerator MoveTo(Stop stop)
    {
        var startPosition = cameraTransform.position;
        var startRotation = cameraTransform.rotation;
        var endPosition = stop.cameraPosition.position;
        var endRotation = LookRotation(stop);

        for (var elapsed = 0f; elapsed < moveSeconds; elapsed += Time.deltaTime)
        {
            var t = Mathf.SmoothStep(0f, 1f, elapsed / moveSeconds);
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, endPosition, t),
                Quaternion.Slerp(startRotation, endRotation, t));
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(endPosition, endRotation);
    }

    private static Quaternion LookRotation(Stop stop)
    {
        return Quaternion.LookRotation(stop.pointOfInterest.position - stop.cameraPosition.position);
    }

    private void OnDrawGizmos()
    {
        if (stops == null)
            return;

        foreach (var stop in stops)
        {
            if (stop.pointOfInterest == null || stop.cameraPosition == null)
                continue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stop.pointOfInterest.position, 0.3f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(stop.cameraPosition.position, Vector3.one * 0.4f);
            Gizmos.DrawLine(stop.cameraPosition.position, stop.pointOfInterest.position);
        }
    }
}
