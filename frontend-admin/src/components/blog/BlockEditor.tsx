import { Plus, Trash2, ArrowUp, ArrowDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";
import type { BlogBlock } from "@/types/blog";

const blockTypes = [
  "heading",
  "paragraph",
  "image",
  "gallery",
  "video",
  "product_spotlight",
  "step",
  "quote",
  "divider",
  "callout",
  "code",
  "embed",
] as const;

function newBlock(type: (typeof blockTypes)[number]): BlogBlock {
  switch (type) {
    case "heading":
      return { type, level: 2, content: "Tiêu đề mới" };
    case "paragraph":
      return { type, content: "Đoạn văn mới..." };
    case "image":
      return {
        type,
        src: "",
        alt: "",
        caption: "",
        width: "contained",
        widthPx: 1200,
        heightPx: 800,
      };
    case "gallery":
      return { type, images: [] };
    case "video":
      return { type, src: "", caption: "" };
    case "product_spotlight":
      return { type, productSlugs: [] };
    case "step":
      return { type, stepNumber: 1, title: "", content: "", tip: "" };
    case "quote":
      return { type, content: "", attribution: "" };
    case "divider":
      return { type };
    case "callout":
      return { type, variant: "tip", content: "" };
    case "code":
      return { type, language: "text", content: "" };
    case "embed":
      return { type, url: "", caption: "" };
  }
}

interface Props {
  blocks: BlogBlock[];
  onChange: (blocks: BlogBlock[]) => void;
}

export function BlockEditor({ blocks, onChange }: Props) {
  function update(index: number, patch: Partial<BlogBlock>) {
    onChange(
      blocks.map((block, i) =>
        i === index ? ({ ...block, ...patch } as BlogBlock) : block,
      ),
    );
  }

  function remove(index: number) {
    onChange(blocks.filter((_, i) => i !== index));
  }

  function move(index: number, dir: -1 | 1) {
    const next = [...blocks];
    const target = index + dir;
    if (target < 0 || target >= next.length) return;
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        {blockTypes.map((type) => (
          <Button
            key={type}
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onChange([...blocks, newBlock(type)])}
          >
            <Plus className="size-3" /> {type}
          </Button>
        ))}
      </div>

      {blocks.map((block, index) => (
        <div
          key={`${block.type}-${index}`}
          className="rounded-xl border bg-white p-4 space-y-3"
        >
          <div className="flex items-center justify-between gap-3">
            <span className="font-medium text-sm uppercase text-primary">
              {index + 1}. {block.type}
            </span>
            <div className="flex gap-1">
              <Button
                type="button"
                size="icon"
                variant="ghost"
                onClick={() => move(index, -1)}
              >
                <ArrowUp className="size-4" />
              </Button>
              <Button
                type="button"
                size="icon"
                variant="ghost"
                onClick={() => move(index, 1)}
              >
                <ArrowDown className="size-4" />
              </Button>
              <Button
                type="button"
                size="icon"
                variant="ghost"
                onClick={() => remove(index)}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </div>
          </div>
          {renderEditor(block, index, update)}
        </div>
      ))}
    </div>
  );
}

function renderEditor(
  block: BlogBlock,
  index: number,
  update: (index: number, patch: Partial<BlogBlock>) => void,
) {
  switch (block.type) {
    case "heading":
      return (
        <>
          <Select
            value={String(block.level)}
            onChange={(e) =>
              update(index, { level: Number(e.target.value) as 1 | 2 | 3 })
            }
          >
            <option value="2">H2</option>
            <option value="3">H3</option>
          </Select>
          <Input
            value={block.content}
            onChange={(e) => update(index, { content: e.target.value })}
          />
        </>
      );
    case "paragraph":
      return (
        <Textarea
          rows={5}
          value={block.content}
          onChange={(e) => update(index, { content: e.target.value })}
        />
      );
    case "image":
      return (
        <div className="grid gap-2 md:grid-cols-2">
          <Input
            placeholder="URL ảnh"
            value={block.src}
            onChange={(e) => update(index, { src: e.target.value })}
          />
          <Input
            placeholder="Alt text SEO"
            value={block.alt}
            onChange={(e) => update(index, { alt: e.target.value })}
          />
          <Input
            placeholder="Caption"
            value={block.caption ?? ""}
            onChange={(e) => update(index, { caption: e.target.value })}
          />
          <Select
            value={block.width ?? "contained"}
            onChange={(e) =>
              update(index, { width: e.target.value as "full" | "contained" })
            }
          >
            <option value="contained">Contained</option>
            <option value="full">Full</option>
          </Select>
        </div>
      );
    case "gallery":
      return (
        <Textarea
          rows={4}
          value={block.images
            .map((i) => `${i.src}|${i.alt}|${i.caption ?? ""}`)
            .join("\n")}
          onChange={(e) =>
            update(index, {
              images: e.target.value
                .split("\n")
                .filter(Boolean)
                .map((line) => {
                  const [src, alt, caption] = line.split("|");
                  return { src, alt: alt ?? "", caption };
                }),
            })
          }
          placeholder="Mỗi dòng: url|alt|caption"
        />
      );
    case "video":
      return (
        <div className="grid gap-2 md:grid-cols-2">
          <Input
            placeholder="URL video"
            value={block.src}
            onChange={(e) => update(index, { src: e.target.value })}
          />
          <Input
            placeholder="Caption"
            value={block.caption ?? ""}
            onChange={(e) => update(index, { caption: e.target.value })}
          />
        </div>
      );
    case "product_spotlight":
      return (
        <Input
          value={block.productSlugs.join(", ")}
          onChange={(e) =>
            update(index, {
              productSlugs: e.target.value
                .split(",")
                .map((s) => s.trim())
                .filter(Boolean),
            })
          }
          placeholder="slug-san-pham-1, slug-san-pham-2"
        />
      );
    case "step":
      return (
        <div className="grid gap-2 md:grid-cols-2">
          <Input
            type="number"
            value={block.stepNumber}
            onChange={(e) =>
              update(index, { stepNumber: Number(e.target.value) })
            }
          />
          <Input
            placeholder="Tiêu đề bước"
            value={block.title}
            onChange={(e) => update(index, { title: e.target.value })}
          />
          <Textarea
            className="md:col-span-2"
            rows={4}
            value={block.content}
            onChange={(e) => update(index, { content: e.target.value })}
          />
          <Input
            className="md:col-span-2"
            placeholder="Mẹo"
            value={block.tip ?? ""}
            onChange={(e) => update(index, { tip: e.target.value })}
          />
        </div>
      );
    case "quote":
      return (
        <div className="grid gap-2">
          <Textarea
            rows={3}
            value={block.content}
            onChange={(e) => update(index, { content: e.target.value })}
          />
          <Input
            placeholder="Nguồn"
            value={block.attribution ?? ""}
            onChange={(e) => update(index, { attribution: e.target.value })}
          />
        </div>
      );
    case "divider":
      return <hr />;
    case "callout":
      return (
        <div className="grid gap-2">
          <Select
            value={block.variant}
            onChange={(e) =>
              update(index, {
                variant: e.target.value as "info" | "warning" | "tip",
              })
            }
          >
            <option value="tip">Tip</option>
            <option value="info">Info</option>
            <option value="warning">Warning</option>
          </Select>
          <Textarea
            rows={3}
            value={block.content}
            onChange={(e) => update(index, { content: e.target.value })}
          />
        </div>
      );
    case "code":
      return (
        <div className="grid gap-2">
          <Input
            value={block.language}
            onChange={(e) => update(index, { language: e.target.value })}
          />
          <Textarea
            rows={6}
            value={block.content}
            onChange={(e) => update(index, { content: e.target.value })}
          />
        </div>
      );
    case "embed":
      return (
        <div className="grid gap-2 md:grid-cols-2">
          <Input
            placeholder="URL embed"
            value={block.url}
            onChange={(e) => update(index, { url: e.target.value })}
          />
          <Input
            placeholder="Caption"
            value={block.caption ?? ""}
            onChange={(e) => update(index, { caption: e.target.value })}
          />
        </div>
      );
  }
}
