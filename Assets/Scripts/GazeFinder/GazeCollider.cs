using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GazeCollider : MonoBehaviour
{
	private readonly HashSet<Collider> overlapping = new HashSet<Collider>();

	private void OnTriggerEnter(Collider other)
	{
		overlapping.Add(other);
	}

	private void OnTriggerExit(Collider other)
	{
		overlapping.Remove(other);
	}

	public void Clear()
	{
		overlapping.Clear();
	}

	public IEnumerable<Collider> GetColliders()
	{
		return overlapping;
	}
}
