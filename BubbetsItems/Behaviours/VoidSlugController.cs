using RoR2;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
	public class VoidSlugController : MonoBehaviour
	{
		private Animator? _animator;
		private CharacterModel? _characterModel;
		private bool _lastInDanger;
		private static readonly int InCombat = Animator.StringToHash("inCombat");

		private void Start()
		{
			_animator = GetComponent<Animator>();
			_characterModel = GetComponentInParent<CharacterModel>();
		}

		private void FixedUpdate()
		{
			if (!_characterModel || !_characterModel!.body || !_animator) return;
			var body = _characterModel.body;
			var inDanger = !body.outOfDanger;
			if (inDanger && _lastInDanger)
			{
				_animator!.SetBool(InCombat, true);
				// Do effect system stuff
			}else if (!inDanger && _lastInDanger)
			{
				_animator!.SetBool(InCombat, false);
				// Do effect system stuff
			}
			_lastInDanger = inDanger;
		}
	}
}