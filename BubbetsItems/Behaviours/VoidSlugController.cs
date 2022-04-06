using System;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	public class VoidSlugController : MonoBehaviour
	{
		private Animator _animator;
		private CharacterModel _characterModel;
		private bool _lastInDanger;

		private void Start()
		{
			_animator = GetComponent<Animator>();
			_characterModel = GetComponentInParent<CharacterModel>();
		}

		private void FixedUpdate()
		{
			if (_characterModel && _characterModel.body && _animator)
			{
				var body = _characterModel.body;
				var inDanger = !body.outOfDanger;
				if (inDanger && _lastInDanger)
				{
					_animator.SetBool("inCombat", true);
					// Do effect system stuff
				}else if (!inDanger && _lastInDanger)
				{
					_animator.SetBool("inCombat", false);
					// Do effect system stuff
				}
				_lastInDanger = inDanger;
			}
		}
	}
}