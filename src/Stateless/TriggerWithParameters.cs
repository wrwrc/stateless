﻿using System;

namespace Stateless
{
    /// <summary>
    /// Associates configured parameters with an underlying trigger value.
    /// </summary>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    public abstract class TriggerWithParameters<TTrigger>
    {
        /// <summary>
        /// 
        /// </summary>
        protected readonly TTrigger _underlyingTrigger;

        /// <summary>
        /// 
        /// </summary>
        protected readonly Type[] _argumentTypes;

        /// <summary>
        /// Create a configured trigger.
        /// </summary>
        /// <param name="underlyingTrigger">Trigger represented by this trigger configuration.</param>
        /// <param name="argumentTypes">The argument types expected by the trigger.</param>
        public TriggerWithParameters(TTrigger underlyingTrigger, params Type[] argumentTypes)
        {
            _underlyingTrigger = underlyingTrigger;
            _argumentTypes = argumentTypes ?? throw new ArgumentNullException(nameof(argumentTypes));
        }

        /// <summary>
        /// Gets the underlying trigger value that has been configured.
        /// </summary>
        public TTrigger Trigger { get { return _underlyingTrigger; } }

        /// <summary>
        /// Ensure that the supplied arguments are compatible with those configured for this
        /// trigger.
        /// </summary>
        /// <param name="args"></param>
        public void ValidateParameters(object[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            ParameterConversion.Validate(args, _argumentTypes);
        }
    }

    /// <summary>
    /// A configured trigger with one required argument.
    /// </summary>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    /// <typeparam name="TArg0">The type of the first argument.</typeparam>
    public class TriggerWithParameters<TTrigger, TArg0> : TriggerWithParameters<TTrigger>
    {
        /// <summary>
        /// Create a configured trigger.
        /// </summary>
        /// <param name="underlyingTrigger">Trigger represented by this trigger configuration.</param>
        public TriggerWithParameters(TTrigger underlyingTrigger)
            : base(underlyingTrigger, typeof(TArg0))
        {
        }
    }

    /// <summary>
    /// A configured trigger with two required arguments.
    /// </summary>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    /// <typeparam name="TArg0">The type of the first argument.</typeparam>
    /// <typeparam name="TArg1">The type of the second argument.</typeparam>
    public class TriggerWithParameters<TTrigger, TArg0, TArg1> : TriggerWithParameters<TTrigger>
    {
        /// <summary>
        /// Create a configured trigger.
        /// </summary>
        /// <param name="underlyingTrigger">Trigger represented by this trigger configuration.</param>
        public TriggerWithParameters(TTrigger underlyingTrigger)
            : base(underlyingTrigger, typeof(TArg0), typeof(TArg1))
        {
        }
    }

    /// <summary>
    /// A configured trigger with three required arguments.
    /// </summary>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    /// <typeparam name="TArg0">The type of the first argument.</typeparam>
    /// <typeparam name="TArg1">The type of the second argument.</typeparam>
    /// <typeparam name="TArg2">The type of the third argument.</typeparam>
    public class TriggerWithParameters<TTrigger, TArg0, TArg1, TArg2> : TriggerWithParameters<TTrigger>
    {
        /// <summary>
        /// Create a configured trigger.
        /// </summary>
        /// <param name="underlyingTrigger">Trigger represented by this trigger configuration.</param>
        public TriggerWithParameters(TTrigger underlyingTrigger)
            : base(underlyingTrigger, typeof(TArg0), typeof(TArg1), typeof(TArg2))
        {
        }
    }
}
