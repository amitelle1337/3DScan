using Intel.RealSense;

namespace _3DScan.Model
{
    public abstract class FilterWrapper
    {
        public bool On { get; set; }
        public abstract ProcessingBlock GetFilter();
    }

    public class DecimationFilterWarpper : FilterWrapper
    {
        public int FilterMagnitude { get; set; }

        public DecimationFilterWarpper(int filterMagnitude = 2)
        {
            FilterMagnitude = filterMagnitude;
        }

        public override ProcessingBlock GetFilter()
        {
            var filter = new DecimationFilter();
            filter.Options[Option.FilterMagnitude].Value = FilterMagnitude;
            return filter;
        }
    }

    public class SpatialFilterWarpper : FilterWrapper
    {
        public int FilterMagnitude { get; set; }
        public float FilterSmoothAlpha { get; set; }
        public int FilterSmoothDelta { get; set; }
        public int HolesFill { get; set; }

        public SpatialFilterWarpper(int filterMagnitude = 2, float filterSmoothAlpha = 0.5f, int filterSmoothDelta = 20, int holesFill = 0)
        {
            FilterMagnitude = filterMagnitude;
            FilterSmoothAlpha = filterSmoothAlpha;
            FilterSmoothDelta = filterSmoothDelta;
            HolesFill = holesFill;
        }

        public override ProcessingBlock GetFilter()
        {
            var filter = new SpatialFilter();
            filter.Options[Option.FilterMagnitude].Value = FilterMagnitude;
            filter.Options[Option.FilterSmoothAlpha].Value = FilterSmoothAlpha;
            filter.Options[Option.FilterSmoothDelta].Value = FilterSmoothDelta;
            filter.Options[Option.HolesFill].Value = HolesFill;
            return filter;
        }
    }

    public class TemporalFilterWrapper : FilterWrapper
    {
        public float FilterSmoothAlpha { get; set; }
        public int FilterSmoothDelta { get; set; }

        public TemporalFilterWrapper(float filterSmoothAlpha = 0.4f, int filterSmoothDelta = 20)
        {
            FilterSmoothAlpha = filterSmoothAlpha;
            FilterSmoothDelta = filterSmoothDelta;
        }

        public override ProcessingBlock GetFilter()
        {
            var filter = new TemporalFilter();
            filter.Options[Option.FilterSmoothAlpha].Value = FilterSmoothAlpha;
            filter.Options[Option.FilterSmoothDelta].Value = FilterSmoothDelta;
            return filter;
        }
    }

    public class HoleFillingFilterWrapper : FilterWrapper
    {
        public int HolesFill { get; set; }

        public HoleFillingFilterWrapper(int holesFill = 1)
        {
            HolesFill = holesFill;
        }

        public override ProcessingBlock GetFilter()
        {
            var filter = new HoleFillingFilter();
            filter.Options[Option.HolesFill].Value = HolesFill;
            return filter;
        }
    }
    public class ThresholdFilterWrapper : FilterWrapper
    {
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }

        public ThresholdFilterWrapper(float minDistance = 0.1f, float maxDistance = 4)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }

        public override ProcessingBlock GetFilter()
        {
            var filter = new ThresholdFilter();
            filter.Options[Option.MinDistance].Value = MinDistance;
            filter.Options[Option.MaxDistance].Value = MaxDistance;
            return filter;
        }
    }
}
