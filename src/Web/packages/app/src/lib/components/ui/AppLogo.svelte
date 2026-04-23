<script lang="ts">
  interface Props {
    /** Icon identifier string (e.g., "dexcom", "xdrip", "loop") or filename with extension (e.g., "mylogo.png") */
    icon: string | undefined;
    /** CSS class applied to the <img> element */
    class?: string;
  }

  const { icon, class: className = "h-5 w-5" }: Props = $props();

  const src = $derived(() => {
    const name = icon ?? "device";
    return name.includes(".") ? `/logos/${name}` : `/logos/${name}.svg`;
  });
</script>

<img
  src={src()}
  alt=""
  class={className}
  draggable="false"
/>
