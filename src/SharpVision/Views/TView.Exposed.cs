using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

// Ported from source/tvision/tvexposd.cpp (TView::exposed).

public partial class TView
{
    /// <summary>Whether any part of the view is visible on the screen (not hidden or covered by other views).</summary>
    public bool Exposed() => new TVExposd().L0(this);

    // A transcription of upstream's register-level algorithm (itself a translation of
    // TVEXPOSD.ASM). The labels and variable names follow upstream to ease comparison.
    private struct TVExposd
    {
        private int _eax, _ebx, _ecx, _esi;
        private TView? _target;

        public bool L0(TView dest)
        {
            if ((dest.State & sfExposed) == 0)
                return false;
            if (0 >= dest.Size.X || 0 >= dest.Size.Y)
                return false;
            return L1(dest);
        }

        private bool L1(TView dest)
        {
            var i = 0;
            do
            {
                _eax = i;
                _ebx = 0;
                _ecx = dest.Size.X;
                if (!L11(dest))
                    return true;
                ++i;
            } while (i < dest.Size.Y);
            return false;
        }

        private bool L10(TView dest)
        {
            var owner = dest.Owner!;
            if (owner.Buffer is not null || owner.LockFlag != 0)
                return false;
            return L11(owner);
        }

        private bool L11(TView dest)
        {
            _target = dest;
            _eax += dest.Origin.Y;
            _ebx += dest.Origin.X;
            _ecx += dest.Origin.X;
            var owner = dest.Owner;
            if (owner is null)
                return false;
            if (_eax < owner.Clip.A.Y)
                return true;
            if (_eax >= owner.Clip.B.Y)
                return true;
            if (_ebx < owner.Clip.A.X)
                _ebx = owner.Clip.A.X;
            return L12(owner);
        }

        private bool L12(TGroup owner)
        {
            if (_ecx > owner.Clip.B.X)
                _ecx = owner.Clip.B.X;
            return L13(owner);
        }

        private bool L13(TGroup owner)
        {
            if (_ebx >= _ecx)
                return true;
            return L20(owner.Last!);
        }

        private bool L20(TView dest)
        {
            var next = dest.Next!;
            if (next == _target)
                return L10(next);
            return L21(next);
        }

        private bool L21(TView next)
        {
            if ((next.State & sfVisible) == 0)
                return L20(next);
            _esi = next.Origin.Y;
            if (_eax < _esi)
                return L20(next);
            _esi += next.Size.Y;
            if (_eax >= _esi)
                return L20(next);
            _esi = next.Origin.X;
            if (_ebx < _esi)
                return L22(next);
            _esi += next.Size.X;
            if (_ebx >= _esi)
                return L20(next);
            _ebx = _esi;
            if (_ebx < _ecx)
                return L20(next);
            return true;
        }

        private bool L22(TView next)
        {
            if (_ecx <= _esi)
                return L20(next);
            _esi += next.Size.X;
            if (_ecx > _esi)
                return L23(next);
            _ecx = next.Origin.X;
            return L20(next);
        }

        private bool L23(TView next)
        {
            var target = _target;
            int esi = _esi, ecx = _ecx, eax = _eax;
            _ecx = next.Origin.X;
            var b = L20(next);
            _eax = eax;
            _ecx = ecx;
            _ebx = esi;
            _target = target;
            if (b)
                return L20(next);
            return false;
        }
    }
}
