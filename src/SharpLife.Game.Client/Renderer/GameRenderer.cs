using Serilog;
using SharpLife.Models.BSP.Loading;
using SharpLife.Models.BSP.Rendering;
using SharpLife.Models.MDL.Loading;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Models.SPR.Loading;
using SharpLife.Models.SPR.Rendering;
using SharpLife.Renderer;
using SharpLife.Renderer.Models;
using SharpLife.Utility;
using System;
using System.Collections.Generic;

namespace SharpLife.Game.Client.Renderer
{
    public class GameRenderer
    {
        private readonly LightStyles _lightStyles = new LightStyles();

        private readonly ILogger _logger;

        private readonly ITime _engineTime;

        private readonly IRenderer _renderer;

        public GameRenderer(ILogger logger, ITime engineTime, IRenderer renderer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public IReadOnlyDictionary<Type, IModelResourceFactory> GetModelResourceFactories()
        {
            return new Dictionary<Type, IModelResourceFactory>
            {
                {typeof(SpriteModel), new SpriteModelResourceFactory(_logger) },
                { typeof(StudioModel), new StudioModelResourceFactory() },
                { typeof(BSPModel), new BSPModelResourceFactory(_renderer, _lightStyles) }
            };
        }

        public void MapLoadBegin()
        {
            //Reset light styles
            _lightStyles.Initialize();
        }

        public void Update()
        {
            _lightStyles.AnimateLights(_engineTime);
        }
    }
}
